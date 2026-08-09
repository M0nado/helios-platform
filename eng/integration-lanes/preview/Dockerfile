FROM python:3.14.0-slim@sha256:0aecac02dc3d4c5dbb024b753af084cafe41f5416e02193f1ce345d671ec966e
WORKDIR /preview
COPY requirements-preview.txt .
ENV PIP_DISABLE_PIP_VERSION_CHECK=1
RUN python -m venv /opt/preview-venv \
    && /opt/preview-venv/bin/pip install --no-cache-dir --require-hashes -r requirements-preview.txt
ENV PATH="/opt/preview-venv/bin:${PATH}" HELIOS_INTEGRATION_LANE=preview
LABEL org.helios.release-artifact="false"
CMD ["python", "-c", "print('HELIOS isolated preview lane ready')"]
