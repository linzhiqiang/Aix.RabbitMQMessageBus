set -ex

cd $(dirname $0)/../

artifactsFolder="./artifacts"

if [ -d $artifactsFolder ]; then
  rm -R $artifactsFolder
fi

mkdir -p $artifactsFolder


dotnet build ./src/Aix.RabbitMQMessageBus/Aix.RabbitMQMessageBus.csproj -c Release

dotnet pack ./src/Aix.RabbitMQMessageBus/Aix.RabbitMQMessageBus.csproj -c Release -o $artifactsFolder

dotnet nuget push ./$artifactsFolder/Aix.RabbitMQMessageBus.*.nupkg -k $PRIVATE_NUGET_KEY -s https://www.nuget.org
