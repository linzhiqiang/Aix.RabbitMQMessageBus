set -ex

cd $(dirname $0)/../

artifactsFolder="./artifacts"

if [ -d $artifactsFolder ]; then
  rm -R $artifactsFolder
fi

mkdir -p $artifactsFolder

dotnet restore ./Aix.RabbitMQMessageBus.sln
dotnet build ./Aix.RabbitMQMessageBus.sln -c Release
