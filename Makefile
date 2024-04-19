PROJECT_NAME = ipk24chat-server

PROJECT_FILE = ./src/$(PROJECT_NAME)/$(PROJECT_NAME).csproj

BUILD_FLAGS = --configuration Release

all: build

build:
	dotnet build $(BUILD_FLAGS) $(PROJECT_FILE)

clean:
	dotnet clean $(PROJECT_FILE)