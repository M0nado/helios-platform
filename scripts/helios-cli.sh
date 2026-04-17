#!/bin/bash

#
# HELIOS CLI Shell Script Wrapper
# Provides bash/sh compatibility for HELIOS platform commands
# Installation: Place in /usr/local/bin or add to PATH
#

set -e

# Determine script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# CLI executable path - adjust based on your installation
if [ -f "$SCRIPT_DIR/helios-cli.exe" ]; then
    CLI_EXEC="$SCRIPT_DIR/helios-cli.exe"
elif [ -f "$SCRIPT_DIR/../bin/Release/net6.0/HELIOS.Platform.CLI.exe" ]; then
    CLI_EXEC="$SCRIPT_DIR/../bin/Release/net6.0/HELIOS.Platform.CLI.exe"
elif command -v helios-cli &> /dev/null; then
    CLI_EXEC="helios-cli"
else
    echo "Error: HELIOS CLI executable not found"
    exit 1
fi

# Ensure .NET runtime is available
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET runtime not found. Please install .NET 6.0 or later"
    exit 1
fi

# Helper functions

show_help() {
    cat << 'EOF'
HELIOS CLI - Platform Management Tool

USAGE:
  helios-cli [OPTIONS] <COMMAND> [ARGS]

COMMANDS:
  deploy      Deploy components or applications
  config      Manage configuration
  status      Show platform status
  health      Check system health
  restart     Restart services
  scale       Scale components
  backup      Create backups
  restore     Restore from backups
  list        List resources
  watch       Watch resource changes
  execute     Execute scripts
  schedule    Schedule tasks
  history     Show command history
  help        Show this help message

OPTIONS:
  -h, --help          Show help message
  -v, --version       Show version
  -q, --quiet         Suppress output
  --verbose           Verbose output
  -j, --json          Output in JSON format
  -o, --output FILE   Write output to file
  --timeout SEC       Command timeout in seconds

EXAMPLES:
  helios-cli status
  helios-cli deploy --config app.json
  helios-cli health --verbose
  helios-cli list --json
  helios-cli scale web --instances 5
  helios-cli backup --path /backups

For more information, visit the documentation.
EOF
}

show_version() {
    echo "HELIOS CLI v1.0.0"
    echo "Platform Version: 7.0.0"
}

# Get platform status
status() {
    "$CLI_EXEC" status "$@"
}

# Check health
health() {
    "$CLI_EXEC" health "$@"
}

# Deploy
deploy() {
    "$CLI_EXEC" deploy "$@"
}

# Manage configuration
config() {
    "$CLI_EXEC" config "$@"
}

# Restart services
restart() {
    "$CLI_EXEC" restart "$@"
}

# Scale components
scale() {
    "$CLI_EXEC" scale "$@"
}

# Backup
backup() {
    "$CLI_EXEC" backup "$@"
}

# Restore
restore() {
    "$CLI_EXEC" restore "$@"
}

# List resources
list() {
    "$CLI_EXEC" list "$@"
}

# Watch resources
watch() {
    "$CLI_EXEC" watch "$@"
}

# Execute scripts
execute() {
    "$CLI_EXEC" execute "$@"
}

# Schedule tasks
schedule() {
    "$CLI_EXEC" schedule "$@"
}

# Show history
history() {
    "$CLI_EXEC" history "$@"
}

# Main execution

if [ $# -eq 0 ]; then
    show_help
    exit 0
fi

case "$1" in
    help|-h|--help)
        show_help
        exit 0
        ;;
    version|-v|--version)
        show_version
        exit 0
        ;;
    -i|--interactive)
        "$CLI_EXEC" -i
        exit $?
        ;;
    -b|--batch)
        if [ -z "$2" ]; then
            echo "Error: Batch file required with --batch flag"
            exit 1
        fi
        "$CLI_EXEC" --batch "$2" "${@:3}"
        exit $?
        ;;
    status|config|deploy|health|restart|scale|backup|restore|list|watch|execute|schedule|history)
        command="$1"
        shift
        $command "$@"
        exit $?
        ;;
    *)
        # Pass through unknown commands to CLI
        "$CLI_EXEC" "$@"
        exit $?
        ;;
esac
