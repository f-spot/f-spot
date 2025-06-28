// F-Spot Data Visualization Dashboard
class FSpotDashboard {
    constructor() {
        this.metrics = this.loadMetrics();
        this.charts = {};
    }

    loadMetrics() {
        return {
            codebase: {
                totalLines: 52000,
                csharpFiles: 150,
                pluginCount: 25,
                testCoverage: 5.2
            },
            architecture: {
                qualityScore: 7.5,
                layers: 5,
                extensionPoints: 12,
                designPatterns: ['Repository', 'Factory', 'Observer', 'Command']
            },
            dependencies: {
                total: 32,
                obsolete: 8,
                critical: 3,
                compatible: 21
            },
            revival: {
                phase1Progress: 15,
                phase2Progress: 0,
                phase3Progress: 0,
                phase4Progress: 0,
                timelineMonths: 18,
                budgetUSD: 200000
            },
            issues: {
                critical: 12,
                high: 28,
                medium: 45,
                low: 67
            }
        };
    }

    renderArchitectureQualityChart(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const quality = this.metrics.architecture.qualityScore;
        const percentage = (quality / 10) * 100;

        container.innerHTML = `
            <div class="quality-chart">
                <div class="quality-circle">
                    <div class="quality-fill" style="--percentage: ${percentage}%">
                        <span class="quality-score">${quality}/10</span>
                    </div>
                </div>
                <div class="quality-details">
                    <h4>Architecture Quality</h4>
                    <p>Strong foundation with excellent separation of concerns</p>
                    <div class="quality-breakdown">
                        <div class="quality-item">
                            <span>Layered Design</span>
                            <div class="quality-bar"><div style="width: 90%"></div></div>
                        </div>
                        <div class="quality-item">
                            <span>Modularity</span>
                            <div class="quality-bar"><div style="width: 85%"></div></div>
                        </div>
                        <div class="quality-item">
                            <span>Extensibility</span>
                            <div class="quality-bar"><div style="width: 80%"></div></div>
                        </div>
                        <div class="quality-item">
                            <span>Maintainability</span>
                            <div class="quality-bar"><div style="width: 65%"></div></div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderDependencyChart(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const deps = this.metrics.dependencies;
        const total = deps.total;

        container.innerHTML = `
            <div class="dependency-chart">
                <div class="dep-donut">
                    <svg viewBox="0 0 42 42" class="donut">
                        <circle cx="21" cy="21" r="15.5" fill="transparent" stroke="#fee2e2" stroke-width="3"></circle>
                        <circle cx="21" cy="21" r="15.5" fill="transparent" stroke="#ef4444" stroke-width="3"
                                stroke-dasharray="${(deps.critical / total) * 97} 97" stroke-dashoffset="25"></circle>
                        <circle cx="21" cy="21" r="15.5" fill="transparent" stroke="#f59e0b" stroke-width="3"
                                stroke-dasharray="${(deps.obsolete / total) * 97} 97"
                                stroke-dashoffset="${25 - (deps.critical / total) * 97}"></circle>
                        <circle cx="21" cy="21" r="15.5" fill="transparent" stroke="#10b981" stroke-width="3"
                                stroke-dasharray="${(deps.compatible / total) * 97} 97"
                                stroke-dashoffset="${25 - ((deps.critical + deps.obsolete) / total) * 97}"></circle>
                    </svg>
                    <div class="donut-center">
                        <span class="donut-number">${total}</span>
                        <span class="donut-label">Dependencies</span>
                    </div>
                </div>
                <div class="dep-legend">
                    <div class="legend-item critical">
                        <span class="legend-color"></span>
                        <span>Critical Blockers (${deps.critical})</span>
                    </div>
                    <div class="legend-item warning">
                        <span class="legend-color"></span>
                        <span>Needs Updates (${deps.obsolete})</span>
                    </div>
                    <div class="legend-item success">
                        <span class="legend-color"></span>
                        <span>Compatible (${deps.compatible})</span>
                    </div>
                </div>
            </div>
        `;
    }

    renderRevivalProgress(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const revival = this.metrics.revival;

        container.innerHTML = `
            <div class="revival-progress">
                <h4>Revival Progress</h4>
                <div class="progress-phases">
                    <div class="phase-item">
                        <div class="phase-header">
                            <span>Phase 1: Stabilization</span>
                            <span class="phase-percentage">${revival.phase1Progress}%</span>
                        </div>
                        <div class="progress-bar">
                            <div class="progress-fill" style="width: ${revival.phase1Progress}%"></div>
                        </div>
                    </div>
                    <div class="phase-item">
                        <div class="phase-header">
                            <span>Phase 2: Modernization</span>
                            <span class="phase-percentage">${revival.phase2Progress}%</span>
                        </div>
                        <div class="progress-bar">
                            <div class="progress-fill" style="width: ${revival.phase2Progress}%"></div>
                        </div>
                    </div>
                    <div class="phase-item">
                        <div class="phase-header">
                            <span>Phase 3: UI Migration</span>
                            <span class="phase-percentage">${revival.phase3Progress}%</span>
                        </div>
                        <div class="progress-bar">
                            <div class="progress-fill" style="width: ${revival.phase3Progress}%"></div>
                        </div>
                    </div>
                    <div class="phase-item">
                        <div class="phase-header">
                            <span>Phase 4: Polish</span>
                            <span class="phase-percentage">${revival.phase4Progress}%</span>
                        </div>
                        <div class="progress-bar">
                            <div class="progress-fill" style="width: ${revival.phase4Progress}%"></div>
                        </div>
                    </div>
                </div>
                <div class="revival-summary">
                    <div class="summary-item">
                        <strong>Timeline</strong>
                        <span>${revival.timelineMonths} months</span>
                    </div>
                    <div class="summary-item">
                        <strong>Budget</strong>
                        <span>$${revival.budgetUSD.toLocaleString()}</span>
                    </div>
                </div>
            </div>
        `;
    }

    renderCodebaseMetrics(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const codebase = this.metrics.codebase;

        container.innerHTML = `
            <div class="codebase-metrics">
                <div class="metric-grid">
                    <div class="metric-card">
                        <div class="metric-value">${(codebase.totalLines / 1000).toFixed(0)}K</div>
                        <div class="metric-label">Lines of Code</div>
                        <div class="metric-trend">↑ Well-structured</div>
                    </div>
                    <div class="metric-card">
                        <div class="metric-value">${codebase.csharpFiles}</div>
                        <div class="metric-label">C# Files</div>
                        <div class="metric-trend">→ Organized</div>
                    </div>
                    <div class="metric-card">
                        <div class="metric-value">${codebase.pluginCount}+</div>
                        <div class="metric-label">Plugins</div>
                        <div class="metric-trend">↑ Extensible</div>
                    </div>
                    <div class="metric-card critical">
                        <div class="metric-value">${codebase.testCoverage}%</div>
                        <div class="metric-label">Test Coverage</div>
                        <div class="metric-trend">↓ Needs work</div>
                    </div>
                </div>
            </div>
        `;
    }

    renderIssueDistribution(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const issues = this.metrics.issues;
        const total = issues.critical + issues.high + issues.medium + issues.low;

        container.innerHTML = `
            <div class="issue-distribution">
                <h4>Issue Distribution</h4>
                <div class="issue-bars">
                    <div class="issue-bar critical">
                        <div class="issue-fill" style="width: ${(issues.critical / total) * 100}%">
                            <span>Critical: ${issues.critical}</span>
                        </div>
                    </div>
                    <div class="issue-bar high">
                        <div class="issue-fill" style="width: ${(issues.high / total) * 100}%">
                            <span>High: ${issues.high}</span>
                        </div>
                    </div>
                    <div class="issue-bar medium">
                        <div class="issue-fill" style="width: ${(issues.medium / total) * 100}%">
                            <span>Medium: ${issues.medium}</span>
                        </div>
                    </div>
                    <div class="issue-bar low">
                        <div class="issue-fill" style="width: ${(issues.low / total) * 100}%">
                            <span>Low: ${issues.low}</span>
                        </div>
                    </div>
                </div>
                <div class="issue-total">
                    <strong>Total Issues: ${total}</strong>
                </div>
            </div>
        `;
    }

    init() {
        // Initialize all dashboard components when DOM is ready
        document.addEventListener('DOMContentLoaded', () => {
            this.renderArchitectureQualityChart('architecture-quality-chart');
            this.renderDependencyChart('dependency-chart');
            this.renderRevivalProgress('revival-progress-chart');
            this.renderCodebaseMetrics('codebase-metrics-chart');
            this.renderIssueDistribution('issue-distribution-chart');
        });
    }
}

// Initialize dashboard
const dashboard = new FSpotDashboard();
dashboard.init();

// Export for use in main app
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FSpotDashboard;
}
