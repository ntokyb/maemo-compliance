using Maemo.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq.Expressions;

namespace Maemo.Infrastructure.Persistence;

/// <summary>
/// EF Core query interceptor that applies tenant filtering to queries at execution time.
/// This replaces the hardcoded tenant ID in query filters with dynamic tenant resolution.
/// </summary>
public class TenantQueryInterceptor : IQueryExpressionInterceptor
{
    private readonly ITenantProvider _tenantProvider;

    public TenantQueryInterceptor(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
    {
        // Get current tenant ID
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // If no tenant is set, skip filtering (allows seeding/admin operations)
        if (tenantId == Guid.Empty)
        {
            return queryExpression;
        }

        // Modify the query expression to add tenant filtering
        var visitor = new TenantFilterVisitor(tenantId);
        return visitor.Visit(queryExpression) ?? queryExpression;
    }

    /// <summary>
    /// Expression visitor that adds tenant filtering to queries for TenantOwnedEntity types.
    /// </summary>
    private class TenantFilterVisitor : ExpressionVisitor
    {
        private readonly Guid _tenantId;

        public TenantFilterVisitor(Guid tenantId)
        {
            _tenantId = tenantId;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Check if this is a queryable method (Where, Select, etc.)
            if (node.Method.DeclaringType == typeof(Queryable) || 
                node.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions))
            {
                // Check if the source is a DbSet<T> where T is TenantOwnedEntity
                if (node.Arguments.Count > 0)
                {
                    var source = node.Arguments[0];
                    var sourceType = source.Type;

                    // Check if source is IQueryable<TenantOwnedEntity>
                    if (sourceType.IsGenericType)
                    {
                        var genericType = sourceType.GetGenericTypeDefinition();
                        if (genericType == typeof(IQueryable<>) || genericType == typeof(DbSet<>))
                        {
                            var entityType = sourceType.GetGenericArguments()[0];
                            if (typeof(Maemo.Domain.Common.TenantOwnedEntity).IsAssignableFrom(entityType))
                            {
                                // Add tenant filter before the existing query
                                var parameter = Expression.Parameter(entityType, "e");
                                var tenantIdProperty = Expression.Property(parameter, nameof(Maemo.Domain.Common.TenantOwnedEntity.TenantId));
                                var tenantIdConstant = Expression.Constant(_tenantId);
                                var equality = Expression.Equal(tenantIdProperty, tenantIdConstant);
                                var lambda = Expression.Lambda(equality, parameter);

                                // Apply Where filter
                                var whereMethod = typeof(Queryable).GetMethods()
                                    .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                                    .MakeGenericMethod(entityType);

                                var filteredSource = Expression.Call(whereMethod, source, lambda);
                                
                                // Replace the source argument with filtered source
                                var newArguments = node.Arguments.ToArray();
                                newArguments[0] = filteredSource;
                                return Expression.Call(node.Object, node.Method, newArguments);
                            }
                        }
                    }
                }
            }

            return base.VisitMethodCall(node);
        }
    }
}
