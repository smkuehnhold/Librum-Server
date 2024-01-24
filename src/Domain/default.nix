{ buildDotnetModule, dotnet-sdk_8, ... }:

buildDotnetModule {
  pname = "librum-server-domain";
  version = "0.0.1";

  src = ./.;

  projectFile = "Domain.csproj";
  nugetDeps = ./deps.nix;
  dotnet-sdk = dotnet-sdk_8;

  # Changes how the output is packaged so that it can be passed to other dotnet-modules via
  #  the projectReferences arg
  packNupkg = true;
}