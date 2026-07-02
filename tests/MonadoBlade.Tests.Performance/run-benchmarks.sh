#!/bin/bash

# MonadoBlade Performance Benchmark Runner
# Automated script to run benchmarks and generate comprehensive reports

# Configuration
DOTNET_VERSION="8.0"
BUILD_CONFIG="Release"
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_DIR="$SCRIPT_DIR"
RESULTS_DIR="$PROJECT_DIR/BenchmarkResults"
TIMESTAMP=$(date +%Y-%m-%d_%H-%M-%S)
RESULTS_TIMESTAMP_DIR="$RESULTS_DIR/$TIMESTAMP"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║     MonadoBlade Performance Benchmarking Suite             ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check .NET is installed
echo -e "${YELLOW}Checking .NET installation...${NC}"
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}ERROR: .NET SDK not found. Please install .NET $DOTNET_VERSION${NC}"
    exit 1
fi

DOTNET_ACTUAL_VERSION=$(dotnet --version)
echo -e "${GREEN}✓ .NET $DOTNET_ACTUAL_VERSION found${NC}"
echo ""

# Create results directory
echo -e "${YELLOW}Setting up results directory...${NC}"
mkdir -p "$RESULTS_TIMESTAMP_DIR"
echo -e "${GREEN}✓ Results will be saved to: $RESULTS_TIMESTAMP_DIR${NC}"
echo ""

# Build project
echo -e "${YELLOW}Building project...${NC}"
cd "$PROJECT_DIR"
if ! dotnet build -c $BUILD_CONFIG > /dev/null 2>&1; then
    echo -e "${RED}ERROR: Build failed${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Build successful${NC}"
echo ""

# Run benchmarks
echo -e "${YELLOW}Running benchmarks...${NC}"
echo -e "${BLUE}Start time: $(date)${NC}"
echo ""

# Run with Release configuration
START_TIME=$(date +%s)

dotnet run -c $BUILD_CONFIG --artifacts "$RESULTS_TIMESTAMP_DIR" 2>&1 | tee "$RESULTS_TIMESTAMP_DIR/benchmark.log"

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

echo ""
echo -e "${BLUE}End time: $(date)${NC}"
echo -e "${BLUE}Total duration: ${DURATION}s${NC}"
echo ""

# Generate summary
echo -e "${YELLOW}Generating summary...${NC}"

if [ -f "$RESULTS_TIMESTAMP_DIR/BENCHMARK_SUMMARY.txt" ]; then
    echo -e "${GREEN}✓ Summary report generated${NC}"
    echo ""
    head -30 "$RESULTS_TIMESTAMP_DIR/BENCHMARK_SUMMARY.txt"
    echo ""
fi

# List generated files
echo -e "${YELLOW}Generated files:${NC}"
echo -e "${GREEN}$(ls -lh "$RESULTS_TIMESTAMP_DIR" | awk 'NR>1 {print "  " $9}')${NC}"
echo ""

# Success message
echo -e "${GREEN}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║             Benchmarking Complete! ✓                       ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "Results saved to: ${BLUE}$RESULTS_TIMESTAMP_DIR${NC}"
echo ""
echo "Next steps:"
echo "  1. Open BenchmarkReport.html in your browser"
echo "  2. Review performance metrics"
echo "  3. Check for regressions against baseline"
echo ""

exit 0
