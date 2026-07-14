FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PORT=8080

WORKDIR /app
COPY pyproject.toml README.md ./
COPY helios_deployment_agent ./helios_deployment_agent
RUN pip install --no-cache-dir . && \
    useradd --create-home --uid 10001 helios

USER 10001
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8080/health', timeout=2)"

CMD ["sh", "-c", "uvicorn helios_deployment_agent.main:app --host 0.0.0.0 --port ${PORT}"]
