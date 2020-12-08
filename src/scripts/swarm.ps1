function build{
    param(
        $registry
    )
    [System.Environment]::SetEnvironmentVariable('DOCKER_REGISTRY', $registry)
    docker-compose build
    docker-compose push
}

function deploy{    
    docker stack deploy --compose-file docker-compose.yml asyncservices
    docker stack services asyncservices
}

build 'localhost:5000/'
deploy
