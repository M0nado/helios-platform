/**
 * USB Installer Module - ENHANCED
 * USB device management, media creation, software installation
 * Features: Device detection, Progress tracking, Error recovery, Retry logic
 * v7.0
 */

const { Logger, Validator, EventEmitter, RetryHandler } = require('../utils');

const TOOLS = [
  'node.js', 'python', 'git', 'vscode', 'docker', 'powershell',
  'brave', 'vlc', 'gimp', 'audacity', '7zip', 'winrar',
  'notepad++', 'sublime', 'cmake', 'mingw', 'rust', 'golang',
  'postgresql', 'mysql', 'mongodb', 'redis', 'rabbitmq', 'kafka',
  'kubernetes', 'terraform', 'ansible', 'prometheus', 'grafana',
  'jenkins', 'gitlab', 'github-cli', 'aws-cli', 'azure-cli',
  'ffmpeg', 'imagemagick', 'ghostscript', 'handbrake', 'obs-studio',
];

class USBInstaller extends EventEmitter {
  constructor(config = {}) {
    super();
    this.config = config;
    this.logger = new Logger('USBInstaller');
    this.usbDevices = [];
    this.installationLog = [];
    this.supportedFormats = ['ISO', 'IMG', 'WIM', 'VHD', 'ESD'];
    this.currentProgress = 0;
    this.retryHandler = new RetryHandler({
      maxRetries: 3,
      initialDelay: 500,
      maxDelay: 5000,
      backoffMultiplier: 2,
    });
    this.logger.info('USB Installer initialized');
  }

  detectUSBDevices() {
    try {
      this.logger.info('Detecting USB devices...');
      
      // Simulated USB detection
      this.usbDevices = [
        { 
          id: `USB_${Date.now()}_001`, 
          name: 'Kingston USB', 
          size: '32GB', 
          status: 'ready',
          detected: new Date(),
        },
        { 
          id: `USB_${Date.now()}_002`, 
          name: 'SanDisk USB', 
          size: '64GB', 
          status: 'ready',
          detected: new Date(),
        },
      ];

      this.logger.info(`Detected ${this.usbDevices.length} USB devices`);
      this.emit('devices-detected', this.usbDevices);
      return this.usbDevices;
    } catch (error) {
      this.logger.error('Failed to detect USB devices', { error: error.message });
      this.emit('error', { action: 'detectUSBDevices', error });
      throw error;
    }
  }

  formatUSB(deviceId, fileSystem = 'NTFS') {
    try {
      const id = Validator.validateString(deviceId, 'deviceId');
      const fs = Validator.validateString(fileSystem, 'fileSystem');
      
      if (!['NTFS', 'FAT32', 'exFAT'].includes(fs)) {
        throw new Error(`Invalid file system: ${fs}`);
      }

      const device = this.usbDevices.find(d => d.id === id);
      if (!device) {
        throw new Error(`USB device ${id} not found`);
      }

      device.fileSystem = fs;
      device.status = 'formatting';
      this.currentProgress = 0;

      this.logger.info(`Formatting USB device ${id} with ${fs}`, { device: id });
      this.emit('format-started', { deviceId: id, fileSystem: fs });

      // Simulate formatting with progress
      for (let i = 0; i <= 100; i += 10) {
        this.currentProgress = i;
        this.emit('format-progress', { deviceId: id, progress: i });
      }

      device.status = 'formatted';
      this.currentProgress = 100;

      const log = {
        action: 'format',
        device: id,
        fileSystem: fs,
        timestamp: new Date(),
        success: true,
      };
      this.installationLog.push(log);

      this.logger.info(`Successfully formatted ${id}`, { fileSystem: fs });
      this.emit('format-completed', { deviceId: id, fileSystem: fs });
      
      return { status: 'success', device, log };
    } catch (error) {
      this.logger.error('Failed to format USB device', { error: error.message });
      this.emit('error', { action: 'formatUSB', error });
      throw error;
    }
  }

  flashImage(deviceId, imagePath, format = 'ISO') {
    try {
      const id = Validator.validateString(deviceId, 'deviceId');
      const path = Validator.validateString(imagePath, 'imagePath');
      Validator.validateString(format, 'format');

      if (!this.supportedFormats.includes(format)) {
        throw new Error(`Unsupported format: ${format}`);
      }

      const device = this.usbDevices.find(d => d.id === id);
      if (!device) {
        throw new Error(`USB device ${id} not found`);
      }

      device.status = 'flashing';
      this.currentProgress = 0;

      this.logger.info(`Flashing image to ${id}`, { image: path, format });
      this.emit('flash-started', { deviceId: id, image: path, format });

      // Simulate flashing with progress
      for (let i = 0; i <= 100; i += 5) {
        this.currentProgress = i;
        this.emit('flash-progress', { deviceId: id, progress: i });
      }

      device.status = 'flashed';
      device.image = path;
      device.imageFormat = format;
      this.currentProgress = 100;

      const log = {
        action: 'flash_image',
        device: id,
        image: path,
        format,
        timestamp: new Date(),
        success: true,
      };
      this.installationLog.push(log);

      this.logger.info(`Successfully flashed ${id}`, { image: path });
      this.emit('flash-completed', { deviceId: id, image: path });
      
      return { status: 'success', device, log };
    } catch (error) {
      this.logger.error('Failed to flash image', { error: error.message });
      this.emit('error', { action: 'flashImage', error });
      throw error;
    }
  }

  async installTool(toolName) {
    try {
      const tool = Validator.validateString(toolName, 'toolName');
      
      if (!TOOLS.includes(tool)) {
        throw new Error(`Tool not found: ${tool}`);
      }

      this.logger.info(`Installing tool: ${tool}`);
      this.emit('install-started', { tool });

      // Retry handler for resilience
      await this.retryHandler.execute(async () => {
        // Simulate installation
        return new Promise((resolve) => {
          setTimeout(() => resolve(true), Math.random() * 500);
        });
      });

      const log = {
        action: 'install_tool',
        tool,
        timestamp: new Date(),
        success: true,
      };
      this.installationLog.push(log);

      this.logger.info(`Successfully installed ${tool}`);
      this.emit('install-completed', { tool });

      return { status: 'success', tool, installed: true, log };
    } catch (error) {
      this.logger.error(`Failed to install tool ${toolName}`, { error: error.message });
      this.emit('error', { action: 'installTool', error });
      throw error;
    }
  }

  async installAll() {
    try {
      this.logger.info(`Installing all ${TOOLS.length} tools...`);
      this.emit('install-all-started', { toolCount: TOOLS.length });

      const results = [];
      for (let i = 0; i < TOOLS.length; i++) {
        const tool = TOOLS[i];
        try {
          const result = await this.installTool(tool);
          results.push(result);
          
          const progress = Math.round((i / TOOLS.length) * 100);
          this.currentProgress = progress;
          this.emit('install-all-progress', { progress, tool, completed: i + 1, total: TOOLS.length });
        } catch (error) {
          this.logger.warn(`Failed to install ${tool}`, { error: error.message });
          results.push({ status: 'failed', tool, error: error.message });
        }
      }

      const log = {
        action: 'install_all',
        toolsInstalled: TOOLS.length,
        timestamp: new Date(),
        success: true,
      };
      this.installationLog.push(log);

      this.currentProgress = 100;
      this.logger.info(`Installation complete: ${TOOLS.length} tools`);
      this.emit('install-all-completed', { results });

      return { status: 'success', totalTools: TOOLS.length, installed: results, log };
    } catch (error) {
      this.logger.error('Failed to install all tools', { error: error.message });
      this.emit('error', { action: 'installAll', error });
      throw error;
    }
  }

  getInstallationLog(limit = 100) {
    return this.installationLog.slice(-limit);
  }

  getProgress() {
    return this.currentProgress;
  }

  getDeviceStatus(deviceId) {
    try {
      const id = Validator.validateString(deviceId, 'deviceId');
      const device = this.usbDevices.find(d => d.id === id);
      return device || null;
    } catch (error) {
      this.logger.error('Failed to get device status', { error: error.message });
      throw error;
    }
  }

  getMetrics() {
    return {
      module: 'usb-installer',
      devicesDetected: this.usbDevices.length,
      toolsAvailable: TOOLS.length,
      installationLogSize: this.installationLog.length,
      currentProgress: this.currentProgress,
      supportedFormats: this.supportedFormats,
      logCount: this.logger.getLogs().length,
      timestamp: Date.now(),
    };
  }
}

module.exports = { USBInstaller, TOOLS };
