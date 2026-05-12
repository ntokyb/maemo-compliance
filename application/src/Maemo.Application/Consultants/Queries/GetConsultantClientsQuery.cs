using Maemo.Application.Consultants.Dtos;
using MediatR;

namespace Maemo.Application.Consultants.Queries;

public class GetConsultantClientsQuery : IRequest<IReadOnlyList<ConsultantClientDto>>
{
}

