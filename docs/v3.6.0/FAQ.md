# HELIOS Platform v3.6.0 - Frequently Asked Questions

**Version**: 3.6.0

## Installation & Setup

**Q: What are the minimum system requirements?**  
A: Windows 11 Pro or Server 2022+, 4GB RAM, 10GB disk space, .NET 8.0, and stable internet connection.

**Q: How long does installation take?**  
A: Typical installation takes 3-5 minutes. First startup takes additional 1-2 minutes for initialization.

**Q: Can I run multiple HELIOS instances?**  
A: Yes, you can run multiple instances with different configurations.

## Cloud Synchronization

**Q: Which cloud providers are supported?**  
A: v3.6.0 supports OneDrive, Azure Blob Storage, AWS S3, and Google Drive.

**Q: How often does automatic sync occur?**  
A: Default is every 5 minutes, configurable from 1 minute to 1 hour.

**Q: What happens if sync fails?**  
A: Failed syncs are automatically retried with exponential backoff. Check Dashboard > Cloud Sync > Logs for details.

**Q: Can I encrypt cloud data?**  
A: Yes, AES-256 encryption is available and recommended.

## Plugins

**Q: Are plugins safe?**  
A: Plugins run in isolated sandboxes with configurable permissions. Always review permissions before installing.

**Q: Can I develop my own plugins?**  
A: Yes, see FEATURES_GUIDE.md > Plugin System for development instructions.

**Q: How do I update a plugin?**  
A: Go to Extensions > Installed Plugins > [Plugin] > "Update" when available.

## Dashboard & UI

**Q: Why can't I access the dashboard?**  
A: Verify HELIOS service is running and port 8080 is not blocked by firewall.

**Q: Is the dashboard secure?**  
A: Yes, dashboard requires authentication and uses HTTPS with TLS 1.3+.

**Q: Can I customize the dashboard?**  
A: Yes, create custom views in Dashboard > + Add View.

**Q: How do I enable dark mode?**  
A: Settings > Appearance > Theme > select "Dark" or "Auto".

## AI/ML Features

**Q: What ML frameworks are supported?**  
A: TensorFlow, PyTorch, ONNX, and custom models.

**Q: Can I use my own trained models?**  
A: Yes, register models via ML Services > Model Management.

## Performance & Optimization

**Q: What's the typical dashboard latency?**  
A: <50ms for page loads, 60fps for real-time updates.

**Q: How much CPU/Memory does HELIOS use?**  
A: Core service: 2-5% CPU, 200-400MB RAM.

**Q: Can I optimize performance further?**  
A: Yes, see DEPLOYMENT.md > Performance Tuning.

## Security

**Q: Is HELIOS suitable for production?**  
A: Yes, v3.6.0 is production-ready with enterprise-grade security.

**Q: What encryption algorithms are used?**  
A: AES-256 for data, PBKDF2 for passwords, TLS 1.3+ for transport.

**Q: Does HELIOS support Active Directory?**  
A: Yes, HELIOS integrates with Windows Active Directory.

## Troubleshooting

**Q: HELIOS service won't start. What do I do?**  
A: Check Windows Event Viewer for errors. Verify appsettings.json is valid JSON.

**Q: My plugins are crashing. How do I debug?**  
A: Check Dashboard > Plugins > [Plugin] > Logs for error messages.

**Q: Cloud sync is very slow. How can I speed it up?**  
A: Check network bandwidth, enable compression, reduce excluded paths.

**Q: Dashboard shows "Unhealthy" status. What does it mean?**  
A: Click on status to see details. Common causes: database disconnect, disk full, high CPU/memory.

## Updates & Maintenance

**Q: How often are updates released?**  
A: Major versions quarterly, minor/patch as needed.

**Q: Can I schedule updates automatically?**  
A: Currently manual, automatic updates planned for v3.7.0.

**Q: What's the backup strategy?**  
A: Daily full backup of database + config recommended.

**Q: How do I rollback to previous version?**  
A: Restore application files and database from backup.

---

**Can't find your answer? Visit GitHub Discussions.**
