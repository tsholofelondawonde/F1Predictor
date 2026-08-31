# Build context is the REPO ROOT, not this directory:
#   docker build -f F1Predictor.WebApi/Dockerfile -t f1predictor-api:local .
# Directory.Build.props and Directory.Packages.props live at the root and central package
# management makes every PackageReference versionless, so a restore without them fails.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore layer: the props files and the six project files WebApi's graph needs, and nothing
# else, so editing source doesn't invalidate the restore cache. AppHost is excluded — it is
# not referenced by WebApi and pulls in the Aspire hosting SDK.
COPY Directory.Build.props Directory.Packages.props ./
COPY F1Predictor.WebApi/F1Predictor.WebApi.csproj                 F1Predictor.WebApi/
COPY F1Predictor.Infrastructure/F1Predictor.Infrastructure.csproj F1Predictor.Infrastructure/
COPY F1Predictor.Application/F1Predictor.Application.csproj       F1Predictor.Application/
COPY F1Predictor.Domain/F1Predictor.Domain.csproj                 F1Predictor.Domain/
COPY F1Predictor.ServiceDefaults/F1Predictor.ServiceDefaults.csproj F1Predictor.ServiceDefaults/
COPY SharedKernel/SharedKernel.csproj                             SharedKernel/
RUN dotnet restore F1Predictor.WebApi/F1Predictor.WebApi.csproj

COPY . .
RUN dotnet publish F1Predictor.WebApi/F1Predictor.WebApi.csproj \
    -c Release -o /app/publish --no-restore

# Not a chiseled or alpine variant: nothing sets InvariantGlobalization, so ICU is required,
# and ML.NET's native libraries want a full glibc userland.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Npgsql probes for GSSAPI/Kerberos encryption before falling back to TLS; the aspnet Debian
# image omits the Kerberos runtime, so that probe throws
# `libgssapi_krb5.so.2: cannot open shared object file` and the Postgres connection fails.
# NpgsqlConnectionStrings.RequireSsl also forces SslMode=Require (which skips the probe) — this
# package is the second line of defence. Placed before the publish COPY so it stays a cached
# layer that source changes don't invalidate.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# The trained models are NOT carried by dotnet publish — F1Predictor.WebApi.csproj declares no
# Content/CopyToOutputDirectory item for them — so they are copied explicitly. Baking them into
# the image is what lets the container app run at min-replicas 0: MlNetRacePredictor loads them
# lazily from disk, and a scaled-to-zero replica has no disk to preserve.
#
# ModelStorageOptions.PathFor resolves the configured "models" via Path.GetFullPath, which is
# relative to the CURRENT WORKING DIRECTORY rather than the content root. WORKDIR /app above is
# therefore load-bearing: change it and MachineLearning:ModelDirectory must change with it.
COPY --chown=$APP_UID:$APP_UID F1Predictor.WebApi/models/ ./models/

# Stamped by CI so /health can prove WHICH commit answered it, not merely that something did.
# `az containerapp update` returns once the update is accepted, and if the new revision fails to
# provision the container app keeps the PREVIOUS one serving — so a /health that only reports
# Healthy cannot tell a successful deploy from a failed one. See the smoke test in
# .github/workflows/build.yml.
#
# Declared in the final stage, after the publish COPY: a new SHA on every commit then invalidates
# only this layer, leaving the restore and publish layers — and so the GHA build cache — intact.
# Move this ARG any earlier and every commit rebuilds the whole image.
ARG GIT_SHA=""
ENV BUILD_SHA=$GIT_SHA

# Non-root. The chown above keeps a `train` call working within a replica's lifetime; the
# result is ephemeral, and retraining for real means committing new .zip files and rebuilding.
USER $APP_UID

# Azure Container Apps routes to a single declared --target-port; 8080 is the convention here.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "F1Predictor.WebApi.dll"]
