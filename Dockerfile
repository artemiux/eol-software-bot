# For more information, please refer to https://aka.ms/vscode-docker-python
FROM python:3.12-slim-bookworm

ARG CONFIG=config.yaml
ARG APPDATA_DIR=var

# Keeps Python from generating .pyc files in the container
ENV PYTHONDONTWRITEBYTECODE=1

# Turns off buffering for easier container logging
ENV PYTHONUNBUFFERED=1

# Install cron and git
RUN apt update && apt install -y cron git

# Install pip requirements
COPY requirements.txt .
RUN python -m pip install -r requirements.txt

WORKDIR /app
COPY . /app
COPY $CONFIG /app/config.yaml
RUN mkdir /app/$APPDATA_DIR

# Creates a Cron job to update data and send notifications
RUN cat >/etc/cron.d/app <<EOF
11 0 * * 1 appuser bash -c '[ "\$EOL_BOT_ENVIRONMENT" != "Development" ] && python /app/src/update.py && python /app/src/send.py'
EOF

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-python-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER root

# During debugging, this entry point will be overridden. For more information, please refer to https://aka.ms/vscode-docker-python-debug
CMD cron && su appuser -c "python /app/main.py"
