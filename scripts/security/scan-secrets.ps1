rg -n "(password|secret|token|connectionstring|client_secret)\s*[:=]" . -g '!docs/status/project-status.yaml'
