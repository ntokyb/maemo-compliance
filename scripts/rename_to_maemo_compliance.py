#!/usr/bin/env python3
"""One-off bulk rename Maemo.* -> MaemoCompliance.* (code + paths). Run from repo root."""
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SKIP_DIRS = {".git", "node_modules", "bin", "obj", "dist", ".angular", "__pycache__"}
TEXT_EXT = {
    ".cs", ".csproj", ".sln", ".json", ".yml", ".yaml", ".md", ".ts", ".html", ".scss",
    ".css", ".conf", ".txt", ".http", ".sh", ".ps1", ".cshtml", ".props", ".targets",
    ".xml",
}


def is_text_path(path: str) -> bool:
    low = path.lower()
    for ext in TEXT_EXT:
        if low.endswith(ext):
            return True
    if os.path.basename(low) in {".env", ".env.example"}:
        return True
    return False


# Ordered replacements (longest / most specific first to avoid partial doubles)
REPLACEMENTS = [
    ("MaemoDbContextModelSnapshot", "MaemoComplianceDbContextModelSnapshot"),
    ("MaemoDbContextFactory", "MaemoComplianceDbContextFactory"),
    ("MaemoDbContext", "MaemoComplianceDbContext"),
    ("MaemoEngineClientOptions", "MaemoComplianceEngineClientOptions"),
    ("MaemoEngineClient", "MaemoComplianceEngineClient"),
    ("MaemoApiFixture", "MaemoComplianceApiFixture"),
    ("MaemoApiCollection", "MaemoComplianceApiCollection"),
    ('CollectionDefinition("MaemoApi"', 'CollectionDefinition("MaemoComplianceApi"'),
    ('Collection("MaemoApi"', 'Collection("MaemoComplianceApi"'),
    # Project / path segments (csproj, sln, docker)
    ("Maemo.Portal.Api", "MaemoCompliance.Portal.Api"),
    ("Maemo.Admin.Api", "MaemoCompliance.Admin.Api"),
    ("Maemo.Engine.Api", "MaemoCompliance.Engine.Api"),
    ("Maemo.Engine.Client", "MaemoCompliance.Engine.Client"),
    ("Maemo.Engine.Sample", "MaemoCompliance.Engine.Sample"),
    ("Maemo.Application", "MaemoCompliance.Application"),
    ("Maemo.Infrastructure", "MaemoCompliance.Infrastructure"),
    ("Maemo.Workers", "MaemoCompliance.Workers"),
    ("Maemo.Shared", "MaemoCompliance.Shared"),
    ("Maemo.Domain", "MaemoCompliance.Domain"),
    ("Maemo.UnitTests", "MaemoCompliance.UnitTests"),
    ("Maemo.IntegrationTests", "MaemoCompliance.IntegrationTests"),
    ("Maemo.Api", "MaemoCompliance.Api"),
    # Namespaces and usings (after path segments so Shared.Application order ok — Application already done)
    ("namespace MaemoComplianceCompliance.", "namespace MaemoCompliance."),  # safety noop if double
    ("using MaemoComplianceCompliance.", "using MaemoCompliance."),
    ("namespace Maemo.", "namespace MaemoCompliance."),
    ("using Maemo.", "using MaemoCompliance."),
    ("global::Maemo.", "global::MaemoCompliance."),
    # RootNamespace in csproj — Maemo.Shared was path-replaced; fix doubled if any
    ("MaemoComplianceCompliance", "MaemoCompliance"),
]

FILE_RENAMES = [
    (
        "infrastructure/src/MaemoCompliance.Infrastructure/Persistence/MaemoDbContext.cs",
        "infrastructure/src/MaemoCompliance.Infrastructure/Persistence/MaemoComplianceDbContext.cs",
    ),
    (
        "infrastructure/src/MaemoCompliance.Infrastructure/Persistence/MaemoDbContextFactory.cs",
        "infrastructure/src/MaemoCompliance.Infrastructure/Persistence/MaemoComplianceDbContextFactory.cs",
    ),
    (
        "infrastructure/src/MaemoCompliance.Infrastructure/Migrations/MaemoDbContextModelSnapshot.cs",
        "infrastructure/src/MaemoCompliance.Infrastructure/Migrations/MaemoComplianceDbContextModelSnapshot.cs",
    ),
    (
        "MaemoCompliance.Engine.Client/MaemoEngineClient.cs",
        "MaemoCompliance.Engine.Client/MaemoComplianceEngineClient.cs",
    ),
    (
        "MaemoCompliance.Engine.Client/MaemoEngineClientOptions.cs",
        "MaemoCompliance.Engine.Client/MaemoComplianceEngineClientOptions.cs",
    ),
    (
        "MaemoCompliance.IntegrationTests/Fixtures/MaemoApiFixture.cs",
        "MaemoCompliance.IntegrationTests/Fixtures/MaemoComplianceApiFixture.cs",
    ),
    (
        "MaemoCompliance.IntegrationTests/Fixtures/MaemoApiCollection.cs",
        "MaemoCompliance.IntegrationTests/Fixtures/MaemoComplianceApiCollection.cs",
    ),
]


def main():
    os.chdir(ROOT)
    for old_rel, new_rel in FILE_RENAMES:
        old = os.path.join(ROOT, old_rel.replace("/", os.sep))
        new = os.path.join(ROOT, new_rel.replace("/", os.sep))
        if os.path.isfile(old):
            os.makedirs(os.path.dirname(new), exist_ok=True)
            os.replace(old, new)
            print("Renamed file:", old_rel, "->", new_rel)

    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS and not d.endswith(".egg-info")]
        for fn in filenames:
            path = os.path.join(dirpath, fn)
            if not is_text_path(path):
                continue
            # Skip huge lock files optional
            if fn == "package-lock.json":
                continue
            try:
                with open(path, "r", encoding="utf-8", newline="") as f:
                    text = f.read()
            except (UnicodeDecodeError, OSError):
                continue
            orig = text
            for old, new in REPLACEMENTS:
                text = text.replace(old, new)
            if text != orig:
                with open(path, "w", encoding="utf-8", newline="") as f:
                    f.write(text)

    print("Content replacements done.")
    sln_old = os.path.join(ROOT, "Maemo.sln")
    sln_new = os.path.join(ROOT, "MaemoCompliance.sln")
    if os.path.isfile(sln_old):
        os.replace(sln_old, sln_new)
        print("Renamed Maemo.sln -> MaemoCompliance.sln")


if __name__ == "__main__":
    main()
