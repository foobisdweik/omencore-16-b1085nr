#!/bin/bash
# OmenCore Linux Tray Integration Helper (#23)
# 
# This script provides system tray integration for OmenCore on Linux.
# It uses libappindicator or systray alternatives available on most DEs.
#
# Requirements:
# - yad (Yet Another Dialog) for GTK tray
# - OR: gir1.2-appindicator3-0.1 for AppIndicator
# - OR: Python with pystray for cross-DE support
#
# Usage:
#   omencore-tray.sh [start|stop|status]
#

SCRIPT_DIR="$(dirname "$(readlink -f "$0")")"
OMENCORE_CLI="${SCRIPT_DIR}/omencore-cli"
ICON_PATH="${SCRIPT_DIR}/icons/omencore.png"
TRAY_PID_FILE="/tmp/omencore-tray.pid"

# Fallback icon if not found
if [ ! -f "$ICON_PATH" ]; then
    ICON_PATH="computer"
fi

# Check for available tray implementation
check_tray_support() {
    if command -v yad &> /dev/null; then
        echo "yad"
    elif python3 -c "import pystray" 2>/dev/null; then
        echo "pystray"
    else
        echo "none"
    fi
}

# Get current status for menu
get_status() {
    if [ -x "$OMENCORE_CLI" ]; then
        "$OMENCORE_CLI" status --json 2>/dev/null || echo '{"error": "Failed to get status"}'
    else
        echo '{"error": "CLI not found"}'
    fi
}

# Format status for display
format_status() {
    local json=$(get_status)
    local cpu_temp=$(echo "$json" | grep -oP '"cpuTemp":\s*\K[0-9]+' 2>/dev/null || echo "?")
    local gpu_temp=$(echo "$json" | grep -oP '"gpuTemp":\s*\K[0-9]+' 2>/dev/null || echo "?")
    local fan_profile=$(echo "$json" | grep -oP '"fanProfile":\s*"\K[^"]+' 2>/dev/null || echo "?")
    echo "CPU: ${cpu_temp}°C | GPU: ${gpu_temp}°C | Fan: ${fan_profile}"
}

# YAD-based tray (GTK)
start_yad_tray() {
    local menu=""
    menu+="Status|$(format_status)|"
    menu+="---||"
    menu+="Fan: Auto|$OMENCORE_CLI fan --profile auto|"
    menu+="Fan: Silent|$OMENCORE_CLI fan --profile silent|"
    menu+="Fan: Gaming|$OMENCORE_CLI fan --profile gaming|"
    menu+="Fan: Max|$OMENCORE_CLI fan --profile max|"
    menu+="---||"
    menu+="Performance: Balanced|$OMENCORE_CLI perf --mode balanced|"
    menu+="Performance: High|$OMENCORE_CLI perf --mode performance|"
    menu+="---||"
    menu+="Open Config|xdg-open ~/.config/omencore/|"
    menu+="Daemon Status|$OMENCORE_CLI daemon --status|"
    menu+="Quit|kill $$|"
    
    yad --notification \
        --image="$ICON_PATH" \
        --text="OmenCore" \
        --menu="$menu" \
        --command="$OMENCORE_CLI status" &
    
    echo $! > "$TRAY_PID_FILE"
    echo "Tray started with PID $!"
}

# Python pystray implementation
start_pystray_tray() {
    python3 << 'PYTHON_SCRIPT'
import pystray
from PIL import Image
import subprocess
import os

def get_status():
    try:
        result = subprocess.run(['omencore-cli', 'status'], capture_output=True, text=True, timeout=5)
        return result.stdout.strip()[:50] if result.returncode == 0 else "Error getting status"
    except:
        return "CLI not available"

def set_fan_profile(profile):
    def inner(icon, item):
        subprocess.run(['omencore-cli', 'fan', '--profile', profile])
    return inner

def set_perf_mode(mode):
    def inner(icon, item):
        subprocess.run(['omencore-cli', 'perf', '--mode', mode])
    return inner

def open_config(icon, item):
    subprocess.run(['xdg-open', os.path.expanduser('~/.config/omencore/')])

def daemon_status(icon, item):
    result = subprocess.run(['omencore-cli', 'daemon', '--status'], capture_output=True, text=True)
    # Could show notification here
    print(result.stdout)

def quit_tray(icon, item):
    icon.stop()

# Create menu
menu = pystray.Menu(
    pystray.MenuItem('Status', lambda: None, enabled=False),
    pystray.Menu.SEPARATOR,
    pystray.MenuItem('Fan: Auto', set_fan_profile('auto')),
    pystray.MenuItem('Fan: Silent', set_fan_profile('silent')),
    pystray.MenuItem('Fan: Gaming', set_fan_profile('gaming')),
    pystray.MenuItem('Fan: Max', set_fan_profile('max')),
    pystray.Menu.SEPARATOR,
    pystray.MenuItem('Performance: Balanced', set_perf_mode('balanced')),
    pystray.MenuItem('Performance: High', set_perf_mode('performance')),
    pystray.Menu.SEPARATOR,
    pystray.MenuItem('Open Config', open_config),
    pystray.MenuItem('Daemon Status', daemon_status),
    pystray.MenuItem('Quit', quit_tray)
)

# Try to load icon
try:
    image = Image.open(os.path.join(os.path.dirname(__file__), 'icons', 'omencore.png'))
except:
    # Create a simple red square as fallback
    image = Image.new('RGB', (64, 64), color='red')

icon = pystray.Icon("omencore", image, "OmenCore", menu)
icon.run()
PYTHON_SCRIPT
    
    echo $! > "$TRAY_PID_FILE"
}

# Stop tray
stop_tray() {
    if [ -f "$TRAY_PID_FILE" ]; then
        local pid=$(cat "$TRAY_PID_FILE")
        if kill -0 "$pid" 2>/dev/null; then
            kill "$pid"
            echo "Tray stopped (PID $pid)"
        fi
        rm -f "$TRAY_PID_FILE"
    else
        echo "Tray not running"
    fi
}

# Check tray status
tray_status() {
    if [ -f "$TRAY_PID_FILE" ]; then
        local pid=$(cat "$TRAY_PID_FILE")
        if kill -0 "$pid" 2>/dev/null; then
            echo "Tray running (PID $pid)"
            return 0
        fi
    fi
    echo "Tray not running"
    return 1
}

# Main
case "${1:-start}" in
    start)
        impl=$(check_tray_support)
        case $impl in
            yad)
                echo "Starting YAD-based tray..."
                start_yad_tray
                ;;
            pystray)
                echo "Starting pystray-based tray..."
                start_pystray_tray
                ;;
            *)
                echo "No tray implementation available."
                echo "Install one of:"
                echo "  - yad (apt install yad)"
                echo "  - Python pystray (pip install pystray pillow)"
                exit 1
                ;;
        esac
        ;;
    stop)
        stop_tray
        ;;
    status)
        tray_status
        ;;
    *)
        echo "Usage: $0 [start|stop|status]"
        exit 1
        ;;
esac
