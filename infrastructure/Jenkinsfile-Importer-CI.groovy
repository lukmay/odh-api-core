pipeline {
    agent any

    environment {
        DOCKER_IMAGE = "app"
        DOCKER_TAG = "latest"
        SERVER_PORT = "1234" 
    }

    stages {
        stage('Configure') {
            steps {
                sh "rm -f .env"
                sh "cp .env.example .env"
                sh "echo 'DOCKER_IMAGE=${DOCKER_IMAGE}' >> .env"
                sh "echo 'DOCKER_TAG=${DOCKER_TAG}' >> .env"
                sh "echo 'SERVER_PORT=${SERVER_PORT}' >> .env"
            }
        }
        stage('Test') {
            steps {
                sh "docker-compose -f O OdhApiImporter/docker-compose.yml build"
            }
        }
    }
    post { 
        always { 
            sh 'docker-compose -f OdhApiImporter/docker-compose.yml down || true'
        }
    }
}