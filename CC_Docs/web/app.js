// F-Spot Documentation Web App
class FSpotDocApp {
    constructor() {
        this.currentSection = 'home';
        this.searchIndex = {};
        this.contentCache = {};
        this.init();
    }

    async init() {
        this.setupNavigation();
        this.setupSearch();
        this.setupKeyboardShortcuts();
        await this.loadDocumentationIndex();
        this.setupAnalytics();
        console.log('F-Spot Documentation App initialized');
    }

    // Navigation System
    setupNavigation() {
        const navLinks = document.querySelectorAll('.nav-link');
        const sections = document.querySelectorAll('.section');

        navLinks.forEach(link => {
            link.addEventListener('click', async (e) => {
                e.preventDefault();
                const targetId = link.getAttribute('href').substring(1);
                await this.navigateToSection(targetId);
            });
        });

        // Handle browser back/forward
        window.addEventListener('popstate', (e) => {
            if (e.state && e.state.section) {
                this.navigateToSection(e.state.section, false);
            }
        });

        // Set initial state
        const hash = window.location.hash.substring(1);
        if (hash) {
            this.navigateToSection(hash, false);
        }
    }

    async navigateToSection(sectionId, updateHistory = true) {
        const targetSection = document.getElementById(sectionId);
        if (!targetSection) return;

        // Track engagement
        this.trackSectionEngagement(sectionId);

        // Update active nav link
        document.querySelectorAll('.nav-link').forEach(link => {
            link.classList.remove('bg-blue-100', 'text-primary');
            link.classList.add('text-gray-700');
        });

        const activeLink = document.querySelector(`[href="#${sectionId}"]`);
        if (activeLink) {
            activeLink.classList.remove('text-gray-700');
            activeLink.classList.add('bg-blue-100', 'text-primary');
        }

        // Show target section
        document.querySelectorAll('.section').forEach(section => {
            section.classList.add('hidden');
            section.classList.remove('active');
        });
        targetSection.classList.remove('hidden');
        targetSection.classList.add('active');

        // Load dynamic content if needed
        await this.loadSectionContent(sectionId);

        // Update browser history
        if (updateHistory) {
            history.pushState({ section: sectionId }, '', `#${sectionId}`);
        }

        this.currentSection = sectionId;

        // Smooth scroll to top
        targetSection.scrollIntoView({ behavior: 'smooth' });

        // Update page title
        document.title = `F-Spot Documentation - ${this.getSectionTitle(sectionId)}`;
    }

    // Dynamic Content Loading
    async loadSectionContent(sectionId) {
        if (this.contentCache[sectionId]) {
            return; // Already loaded
        }

        const contentMap = {
            'architecture': () => this.loadArchitectureContent(),
            'domain': () => this.loadDomainModelContent(),
            'database': () => this.loadDatabaseContent(),
            'services': () => this.loadServicesContent(),
            'ui': () => this.loadUIContent(),
            'plugins': () => this.loadPluginsContent(),
            'dataflow': () => this.loadDataFlowContent(),
            'dependencies': () => this.loadDependenciesContent(),
            'roadmap': () => this.loadRoadmapContent()
        };

        const loader = contentMap[sectionId];
        if (loader) {
            await loader();
            this.contentCache[sectionId] = true;
        }
    }

    // Search Functionality
    setupSearch() {
        const searchInput = document.getElementById('search-input');
        const searchResults = document.getElementById('search-results');

        if (!searchInput) return;

        let searchTimeout;
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                this.performSearch(e.target.value);
            }, 300);
        });

        // Close search results when clicking outside
        document.addEventListener('click', (e) => {
            if (!searchInput.contains(e.target) && !searchResults.contains(e.target)) {
                searchResults.style.display = 'none';
            }
        });
    }

    async performSearch(query) {
        const searchResults = document.getElementById('search-results');
        if (!searchResults) return;

        if (query.length < 2) {
            searchResults.style.display = 'none';
            return;
        }

        const results = this.searchContent(query);
        this.displaySearchResults(results);
    }

    searchContent(query) {
        const results = [];
        const lowercaseQuery = query.toLowerCase();

        // Search through predefined content
        const searchableContent = {
            'home': 'F-Spot Photo Manager personal photo management GNOME desktop revival modernization',
            'features': 'photo organization import tagging rating search editing crop rotate color effects export sharing',
            'architecture': 'system architecture layered design domain model service layer presentation plugins',
            'database': 'SQLite schema repositories data access patterns migrations versioning',
            'plugins': 'Mono.Addins extension points editors exporters tools modularity',
            'roadmap': 'revival strategy modernization timeline phases stabilization migration'
        };

        Object.entries(searchableContent).forEach(([section, content]) => {
            if (content.toLowerCase().includes(lowercaseQuery)) {
                results.push({
                    section,
                    title: this.getSectionTitle(section),
                    snippet: this.extractSnippet(content, query)
                });
            }
        });

        return results;
    }

    displaySearchResults(results) {
        const searchResults = document.getElementById('search-results');
        if (!searchResults) return;

        if (results.length === 0) {
            searchResults.innerHTML = '<div class="search-no-results">No results found</div>';
        } else {
            searchResults.innerHTML = results.map(result => `
                <div class="search-result" onclick="app.navigateToSection('${result.section}')">
                    <div class="search-result-title">${result.title}</div>
                    <div class="search-result-snippet">${result.snippet}</div>
                </div>
            `).join('');
        }

        searchResults.style.display = 'block';
    }

    getSectionTitle(sectionId) {
        const titles = {
            'home': 'Home',
            'features': 'Features',
            'architecture': 'System Architecture',
            'domain': 'Domain Model',
            'database': 'Database Layer',
            'services': 'Service Layer',
            'ui': 'UI Components',
            'plugins': 'Plugin System',
            'dataflow': 'Data Flow',
            'dependencies': 'Dependencies',
            'roadmap': 'Revival Roadmap'
        };
        return titles[sectionId] || sectionId;
    }

    extractSnippet(content, query) {
        const index = content.toLowerCase().indexOf(query.toLowerCase());
        if (index === -1) return content.substring(0, 100) + '...';

        const start = Math.max(0, index - 30);
        const end = Math.min(content.length, index + query.length + 30);
        return '...' + content.substring(start, end) + '...';
    }

    // Keyboard Shortcuts
    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            if (e.altKey) {
                const sections = ['home', 'features', 'download', 'documentation', 'community', 'development',
                                'architecture', 'domain', 'database', 'services', 'ui', 'plugins', 'dataflow',
                                'dependencies', 'roadmap'];
                const currentIndex = sections.indexOf(this.currentSection);

                if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') {
                    e.preventDefault();
                    const prevIndex = currentIndex > 0 ? currentIndex - 1 : sections.length - 1;
                    this.navigateToSection(sections[prevIndex]);
                } else if (e.key === 'ArrowDown' || e.key === 'ArrowRight') {
                    e.preventDefault();
                    const nextIndex = currentIndex < sections.length - 1 ? currentIndex + 1 : 0;
                    this.navigateToSection(sections[nextIndex]);
                }
            }
        });
    }

    // Content Loaders for Dynamic Sections
    async loadArchitectureContent() {
        const section = document.getElementById('architecture');
        if (!section) return;

        const architectureHTML = `
            <h1>System Architecture</h1>
            <div class="architecture-overview">
                <div class="arch-summary">
                    <h3>Architecture Quality Score: 7.5/10</h3>
                    <p>F-Spot demonstrates excellent architectural principles with clear separation of concerns,
                    though it suffers from some legacy UI coupling issues.</p>
                </div>
            </div>

            <div class="architecture-diagram">
                <div class="layer presentation">
                    <h3>Presentation Layer</h3>
                    <div class="components">
                        <div class="component main-window">MainWindow<br><small>Central UI Controller</small></div>
                        <div class="component dialogs">Dialogs<br><small>Modal Interactions</small></div>
                        <div class="component widgets">Custom Widgets<br><small>Photo Viewing</small></div>
                    </div>
                </div>
                <div class="layer plugins">
                    <h3>Plugin System</h3>
                    <div class="components">
                        <div class="component editors">Editors<br><small>Image Processing</small></div>
                        <div class="component exporters">Exporters<br><small>Format Conversion</small></div>
                        <div class="component tools">Tools<br><small>Utilities</small></div>
                    </div>
                </div>
                <div class="layer services">
                    <h3>Service Layer</h3>
                    <div class="components">
                        <div class="component import">Import Service<br><small>File Processing</small></div>
                        <div class="component imaging">Imaging Service<br><small>Core Operations</small></div>
                        <div class="component thumbnail">Thumbnail Service<br><small>Preview Generation</small></div>
                    </div>
                </div>
                <div class="layer domain">
                    <h3>Domain Layer</h3>
                    <div class="components">
                        <div class="component photo">Photo Entity<br><small>Core Model</small></div>
                        <div class="component tag">Tag System<br><small>Organization</small></div>
                        <div class="component query">Query Engine<br><small>Search Logic</small></div>
                    </div>
                </div>
                <div class="layer data">
                    <h3>Data Access Layer</h3>
                    <div class="components">
                        <div class="component repositories">Repositories<br><small>Data Access</small></div>
                        <div class="component sqlite">SQLite DB<br><small>Storage</small></div>
                        <div class="component filesystem">File System<br><small>Media Storage</small></div>
                    </div>
                </div>
            </div>

            <div class="architecture-details">
                <div class="detail-card">
                    <h3>🏗️ Layered Architecture Benefits</h3>
                    <ul>
                        <li>Clear separation of concerns</li>
                        <li>Testable business logic</li>
                        <li>Pluggable extension system</li>
                        <li>Repository pattern for data access</li>
                    </ul>
                </div>
                <div class="detail-card">
                    <h3>⚠️ Current Issues</h3>
                    <ul>
                        <li>UI framework obsolescence (GTK# 2.12)</li>
                        <li>MainWindow as "God Class"</li>
                        <li>Synchronous I/O operations</li>
                        <li>Legacy dependency injection</li>
                    </ul>
                </div>
                <div class="detail-card">
                    <h3>🎯 Modernization Targets</h3>
                    <ul>
                        <li>Migrate to modern UI framework</li>
                        <li>Implement MVVM pattern</li>
                        <li>Add async/await patterns</li>
                        <li>Upgrade dependency injection</li>
                    </ul>
                </div>
            </div>
        `;

        section.innerHTML = architectureHTML;
    }

    async loadDomainModelContent() {
        const section = document.getElementById('domain');
        if (!section) return;

        const domainHTML = `
            <h1>Domain Model</h1>
            <div class="domain-overview">
                <p>F-Spot's domain model is well-designed with clear entity relationships and good encapsulation of business rules.</p>
            </div>

            <div class="domain-model">
                <div class="entity-group core">
                    <h3>Core Entities</h3>
                    <div class="entity photo-entity">
                        <h4>Photo</h4>
                        <div class="properties">
                            <div class="property">Id: uint</div>
                            <div class="property">Time: DateTime</div>
                            <div class="property">Uri: SafeUri</div>
                            <div class="property">Description: string</div>
                            <div class="property">Rating: uint</div>
                        </div>
                        <div class="methods">
                            <div class="method">GetVersion()</div>
                            <div class="method">CreateVersion()</div>
                            <div class="method">DeleteVersion()</div>
                        </div>
                    </div>
                    <div class="entity tag-entity">
                        <h4>Tag</h4>
                        <div class="properties">
                            <div class="property">Id: uint</div>
                            <div class="property">Name: string</div>
                            <div class="property">CategoryId: uint</div>
                            <div class="property">IsCategory: bool</div>
                        </div>
                    </div>
                </div>

                <div class="entity-group metadata">
                    <h3>Metadata</h3>
                    <div class="entity version-entity">
                        <h4>PhotoVersion</h4>
                        <div class="properties">
                            <div class="property">PhotoId: uint</div>
                            <div class="property">VersionId: uint</div>
                            <div class="property">Name: string</div>
                            <div class="property">Uri: SafeUri</div>
                            <div class="property">Protected: bool</div>
                        </div>
                    </div>
                    <div class="entity import-entity">
                        <h4>ImportSession</h4>
                        <div class="properties">
                            <div class="property">Time: DateTime</div>
                            <div class="property">Source: string</div>
                            <div class="property">PhotoCount: int</div>
                        </div>
                    </div>
                </div>

                <div class="relationships">
                    <h3>Entity Relationships</h3>
                    <div class="relationship">
                        <strong>Photo ←→ Tag</strong>
                        <span>Many-to-Many (PhotoTags junction)</span>
                    </div>
                    <div class="relationship">
                        <strong>Photo ←→ PhotoVersion</strong>
                        <span>One-to-Many (versioning system)</span>
                    </div>
                    <div class="relationship">
                        <strong>Tag ←→ Tag</strong>
                        <span>Hierarchical (parent-child categories)</span>
                    </div>
                </div>
            </div>

            <div class="domain-analysis">
                <div class="analysis-card">
                    <h3>✅ Domain Strengths</h3>
                    <ul>
                        <li>Rich entity model with proper encapsulation</li>
                        <li>Non-destructive editing through versioning</li>
                        <li>Flexible tagging with hierarchical categories</li>
                        <li>Comprehensive metadata support</li>
                    </ul>
                </div>
                <div class="analysis-card">
                    <h3>🔄 Modernization Opportunities</h3>
                    <ul>
                        <li>Add async repository patterns</li>
                        <li>Implement domain events</li>
                        <li>Add value objects for better encapsulation</li>
                        <li>Consider CQRS for complex queries</li>
                    </ul>
                </div>
            </div>
        `;

        section.innerHTML = domainHTML;
    }

    async loadDatabaseContent() {
        const section = document.getElementById('database');
        if (!section) return;

        const databaseHTML = `
            <h1>Database Layer</h1>
            <div class="database-overview">
                <div class="db-stats">
                    <div class="stat">
                        <strong>Engine</strong>
                        <span>SQLite 2.8.6+</span>
                    </div>
                    <div class="stat">
                        <strong>Schema Versions</strong>
                        <span>18+ Migrations</span>
                    </div>
                    <div class="stat">
                        <strong>Tables</strong>
                        <span>8 Core Tables</span>
                    </div>
                </div>
            </div>

            <div class="database-schema">
                <h3>Database Schema</h3>
                <div class="schema-diagram">
                    <div class="table photos-table">
                        <h4>photos</h4>
                        <div class="columns">
                            <div class="column pk">id INTEGER PRIMARY KEY</div>
                            <div class="column">time INTEGER</div>
                            <div class="column">base_uri STRING</div>
                            <div class="column">filename STRING</div>
                            <div class="column">description TEXT</div>
                            <div class="column">default_version_id INTEGER</div>
                            <div class="column">rating INTEGER</div>
                        </div>
                    </div>

                    <div class="table tags-table">
                        <h4>tags</h4>
                        <div class="columns">
                            <div class="column pk">id INTEGER PRIMARY KEY</div>
                            <div class="column">name STRING UNIQUE</div>
                            <div class="column">category_id INTEGER</div>
                            <div class="column">is_category BOOLEAN</div>
                            <div class="column">sort_priority INTEGER</div>
                        </div>
                    </div>

                    <div class="table photo-tags-table">
                        <h4>photo_tags</h4>
                        <div class="columns">
                            <div class="column pk">photo_id INTEGER</div>
                            <div class="column pk">tag_id INTEGER</div>
                        </div>
                    </div>

                    <div class="table photo-versions-table">
                        <h4>photo_versions</h4>
                        <div class="columns">
                            <div class="column pk">photo_id INTEGER</div>
                            <div class="column pk">version_id INTEGER</div>
                            <div class="column">name STRING</div>
                            <div class="column">base_uri STRING</div>
                            <div class="column">filename STRING</div>
                            <div class="column">protected BOOLEAN</div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="repository-pattern">
                <h3>Repository Implementation</h3>
                <div class="repo-examples">
                    <div class="repo-card">
                        <h4>PhotoStore</h4>
                        <p>Handles photo CRUD operations with connection management and transaction support.</p>
                        <div class="code-sample">
                            <code>
                                // Example query pattern<br>
                                SELECT p.*, t.name as tag_name<br>
                                FROM photos p<br>
                                LEFT JOIN photo_tags pt ON p.id = pt.photo_id<br>
                                LEFT JOIN tags t ON pt.tag_id = t.id<br>
                                WHERE p.rating >= ?
                            </code>
                        </div>
                    </div>
                    <div class="repo-card">
                        <h4>TagStore</h4>
                        <p>Manages hierarchical tag structure with category support and efficient querying.</p>
                    </div>
                </div>
            </div>

            <div class="database-issues">
                <div class="issue-card critical">
                    <h3>🚨 Critical Issues</h3>
                    <ul>
                        <li><strong>SQL Injection Vulnerabilities:</strong> Direct string concatenation in queries</li>
                        <li><strong>Legacy SQLite:</strong> Using very old version (2.8.6)</li>
                        <li><strong>Synchronous Operations:</strong> Blocking UI during database operations</li>
                    </ul>
                </div>
                <div class="issue-card warning">
                    <h3>⚠️ Modernization Needs</h3>
                    <ul>
                        <li>Implement proper parameterized queries</li>
                        <li>Add async/await database operations</li>
                        <li>Consider Entity Framework or Dapper</li>
                        <li>Implement connection pooling</li>
                    </ul>
                </div>
            </div>
        `;

        section.innerHTML = databaseHTML;
    }

    async loadRoadmapContent() {
        const section = document.getElementById('roadmap');
        if (!section) return;

        const roadmapHTML = `
            <h1>Revival Roadmap</h1>
            <div class="roadmap-overview">
                <div class="timeline-summary">
                    <h3>Complete Modernization Timeline: 15-21 Months</h3>
                    <p>Systematic approach to reviving F-Spot with modern technologies while preserving core functionality.</p>
                </div>
            </div>

            <div class="roadmap-phases">
                <div class="phase phase-1">
                    <div class="phase-header">
                        <h3>Phase 1: Emergency Stabilization</h3>
                        <span class="duration">2-3 Months</span>
                        <span class="status current">Current Phase</span>
                    </div>
                    <div class="phase-content">
                        <div class="objectives">
                            <h4>Primary Objectives</h4>
                            <ul>
                                <li>Get F-Spot building and running on modern systems</li>
                                <li>Fix critical security vulnerabilities</li>
                                <li>Establish CI/CD pipeline</li>
                                <li>Create comprehensive test suite</li>
                            </ul>
                        </div>
                        <div class="deliverables">
                            <h4>Key Deliverables</h4>
                            <div class="deliverable">
                                <strong>Build System Repair</strong>
                                <ul>
                                    <li>Fix GTK# 2.12 dependencies</li>
                                    <li>Resolve platform detection issues</li>
                                    <li>Consolidate to MSBuild</li>
                                </ul>
                            </div>
                            <div class="deliverable">
                                <strong>Security Fixes</strong>
                                <ul>
                                    <li>Fix SQL injection vulnerabilities</li>
                                    <li>Add input validation</li>
                                    <li>Secure file handling</li>
                                </ul>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="phase phase-2">
                    <div class="phase-header">
                        <h3>Phase 2: Architecture Modernization</h3>
                        <span class="duration">4-6 Months</span>
                        <span class="status planned">Planned</span>
                    </div>
                    <div class="phase-content">
                        <div class="objectives">
                            <h4>Primary Objectives</h4>
                            <ul>
                                <li>Migrate to .NET 6+</li>
                                <li>Implement async/await patterns</li>
                                <li>Modernize data access layer</li>
                                <li>Refactor service layer</li>
                            </ul>
                        </div>
                        <div class="tech-migrations">
                            <h4>Technology Migrations</h4>
                            <div class="migration">
                                <strong>Data Access:</strong> SQLite → Entity Framework Core
                            </div>
                            <div class="migration">
                                <strong>DI Container:</strong> Custom → Microsoft.Extensions.DependencyInjection
                            </div>
                            <div class="migration">
                                <strong>Logging:</strong> Console → Microsoft.Extensions.Logging
                            </div>
                        </div>
                    </div>
                </div>

                <div class="phase phase-3">
                    <div class="phase-header">
                        <h3>Phase 3: UI Framework Migration</h3>
                        <span class="duration">6-8 Months</span>
                        <span class="status planned">Planned</span>
                    </div>
                    <div class="phase-content">
                        <div class="ui-options">
                            <h4>UI Framework Options</h4>
                            <div class="ui-option recommended">
                                <strong>Avalonia UI (Recommended)</strong>
                                <ul>
                                    <li>Cross-platform XAML framework</li>
                                    <li>Similar to WPF development model</li>
                                    <li>Excellent Linux support</li>
                                    <li>Strong MVVM capabilities</li>
                                </ul>
                            </div>
                            <div class="ui-option alternative">
                                <strong>GTK 4 + .NET</strong>
                                <ul>
                                    <li>Native GNOME integration</li>
                                    <li>Maintains Linux-first approach</li>
                                    <li>Modern GTK features</li>
                                </ul>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="phase phase-4">
                    <div class="phase-header">
                        <h3>Phase 4: Performance & Polish</h3>
                        <span class="duration">3-4 Months</span>
                        <span class="status planned">Planned</span>
                    </div>
                    <div class="phase-content">
                        <div class="objectives">
                            <h4>Final Objectives</h4>
                            <ul>
                                <li>Performance optimization</li>
                                <li>Memory leak elimination</li>
                                <li>Enhanced user experience</li>
                                <li>Plugin system modernization</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>

            <div class="success-metrics">
                <h3>Success Metrics</h3>
                <div class="metrics-grid">
                    <div class="metric">
                        <div class="metric-value">100%</div>
                        <div class="metric-label">Build Success Rate</div>
                    </div>
                    <div class="metric">
                        <div class="metric-value">< 2s</div>
                        <div class="metric-label">Application Startup</div>
                    </div>
                    <div class="metric">
                        <div class="metric-value">80%+</div>
                        <div class="metric-label">Test Coverage</div>
                    </div>
                    <div class="metric">
                        <div class="metric-value">0</div>
                        <div class="metric-label">Critical Security Issues</div>
                    </div>
                </div>
            </div>

            <div class="risk-assessment">
                <h3>Risk Assessment & Mitigation</h3>
                <div class="risks">
                    <div class="risk high">
                        <strong>High Risk:</strong> UI Framework Migration Complexity
                        <p><strong>Mitigation:</strong> Parallel UI development, gradual migration strategy</p>
                    </div>
                    <div class="risk medium">
                        <strong>Medium Risk:</strong> Plugin Compatibility Breaking
                        <p><strong>Mitigation:</strong> Plugin adapter layer, community engagement</p>
                    </div>
                    <div class="risk low">
                        <strong>Low Risk:</strong> Performance Regression
                        <p><strong>Mitigation:</strong> Continuous benchmarking, optimization focus</p>
                    </div>
                </div>
            </div>
        `;

        section.innerHTML = roadmapHTML;
    }

    // Analytics and Tracking
    setupAnalytics() {
        // Track section views
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    console.log(`Section viewed: ${entry.target.id}`);
                    // Here you could send analytics data
                }
            });
        });

        document.querySelectorAll('.section').forEach(section => {
            observer.observe(section);
        });
    }

    // Documentation Index Loading
    async loadDocumentationIndex() {
        // Comprehensive content index for better search
        this.searchIndex = {
            'home': 'F-Spot Photo Manager personal photo management GNOME desktop revival modernization overview features statistics architecture quality',
            'features': 'photo organization import tagging rating search editing crop rotate color effects export sharing non-destructive versioning RAW JPEG',
            'download': 'installation build requirements GTK# Mono SQLite dependencies system requirements Ubuntu Linux',
            'documentation': 'user guide technical documentation revival strategy API reference multilingual help system',
            'community': 'GitHub issues Gitter chat contributing development testing translation community support',
            'development': 'build status CI/CD pipeline development setup priority tasks project metrics codebase structure',
            'architecture': 'system architecture layered design domain model service layer presentation plugins separation concerns repository pattern',
            'domain': 'domain model entities relationships Photo Tag Version metadata business logic encapsulation hierarchy',
            'database': 'SQLite schema repository pattern data access migrations versioning database layer persistence storage queries',
            'services': 'service layer business logic import imaging thumbnail query cross-cutting concerns dependency injection',
            'ui': 'user interface GTK# components MainWindow widgets legacy framework UI migration Avalonia MVVM patterns',
            'plugins': 'Mono.Addins extension points modularity editors exporters tools importers plugin system architecture interfaces',
            'dataflow': 'data flow patterns command observer factory repository user interaction import edit process workflows',
            'dependencies': 'external dependencies GTK# Mono SQLite LCMS libraries frameworks migration strategy compatibility',
            'roadmap': 'revival strategy modernization timeline phases stabilization architecture UI migration performance timeline budget',
            'status': 'current status build failures critical issues priorities progress tracking metrics non-functional state'
        };
    }

    trackSectionEngagement(sectionId) {
        const engagement = JSON.parse(localStorage.getItem('fspot-engagement') || '{}');
        engagement[sectionId] = (engagement[sectionId] || 0) + 1;
        engagement[`${sectionId}_lastVisited`] = new Date().toISOString();
        localStorage.setItem('fspot-engagement', JSON.stringify(engagement));
    }

    // Enhanced Plugin System Content
    async loadPluginsContent() {
        const section = document.getElementById('plugins');
        if (!section) return;

        const pluginsHTML = `
            <h1>Plugin System</h1>
            <div class="plugins-overview">
                <div class="plugin-stats">
                    <div class="stat">
                        <strong>Framework</strong>
                        <span>Mono.Addins</span>
                    </div>
                    <div class="stat">
                        <strong>Total Plugins</strong>
                        <div class="score">25+</div>
                        <span>Across 4 categories</span>
                    </div>
                    <div class="stat">
                        <strong>Extension Points</strong>
                        <div class="score">12</div>
                        <span>Well-defined interfaces</span>
                    </div>
                </div>
            </div>

            <div class="plugin-architecture">
                <h3>Plugin Architecture</h3>
                <div class="arch-diagram">
                    <div class="plugin-host">
                        <h4>F-Spot Core Application</h4>
                        <div class="extension-points">
                            <div class="extension-point">IImageEditor</div>
                            <div class="extension-point">IExporter</div>
                            <div class="extension-point">ITool</div>
                            <div class="extension-point">IImporter</div>
                        </div>
                    </div>
                    <div class="plugin-types">
                        <div class="plugin-type editors">
                            <h4>📝 Editors (8)</h4>
                            <div class="plugin-list">
                                <div class="plugin">Crop Editor</div>
                                <div class="plugin">Resize Editor</div>
                                <div class="plugin">Rotate Editor</div>
                                <div class="plugin">Color Editor</div>
                                <div class="plugin">Auto Color</div>
                                <div class="plugin">Red Eye Removal</div>
                                <div class="plugin">Desaturate</div>
                                <div class="plugin">Sepia Tone</div>
                            </div>
                        </div>
                        <div class="plugin-type exporters">
                            <h4>🌐 Exporters (10)</h4>
                            <div class="plugin-list">
                                <div class="plugin">Flickr Export</div>
                                <div class="plugin">Facebook Export</div>
                                <div class="plugin">PicasaWeb Export</div>
                                <div class="plugin">SmugMug Export</div>
                                <div class="plugin">Gallery Export</div>
                                <div class="plugin">Folder Export</div>
                                <div class="plugin">CD Export</div>
                                <div class="plugin">Web Gallery</div>
                                <div class="plugin">Email Export</div>
                                <div class="plugin">Print Export</div>
                            </div>
                        </div>
                        <div class="plugin-type tools">
                            <h4>🛠️ Tools (5)</h4>
                            <div class="plugin-list">
                                <div class="plugin">Screensaver Tool</div>
                                <div class="plugin">Slideshow Tool</div>
                                <div class="plugin">Metadata Tool</div>
                                <div class="plugin">Synchronization Tool</div>
                                <div class="plugin">Development Tool</div>
                            </div>
                        </div>
                        <div class="plugin-type importers">
                            <h4>📥 Importers (2)</h4>
                            <div class="plugin-list">
                                <div class="plugin">Camera Importer</div>
                                <div class="plugin">File Importer</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="plugin-details">
                <div class="detail-section">
                    <h3>🔌 Extension Points</h3>
                    <div class="extension-details">
                        <div class="extension-card">
                            <h4>IImageEditor</h4>
                            <p>Provides image editing capabilities with version management</p>
                            <div class="interface-methods">
                                <code>bool CanEdit(Photo photo)</code><br>
                                <code>void Edit(Photo photo, Gtk.Window parent)</code><br>
                                <code>string Name { get; }</code><br>
                                <code>string MenuLabel { get; }</code>
                            </div>
                        </div>
                        <div class="extension-card">
                            <h4>IExporter</h4>
                            <p>Handles photo export to various destinations and formats</p>
                            <div class="interface-methods">
                                <code>void Run(IBrowsableCollection photos)</code><br>
                                <code>string Name { get; }</code><br>
                                <code>string IconName { get; }</code>
                            </div>
                        </div>
                        <div class="extension-card">
                            <h4>ITool</h4>
                            <p>Provides utility tools for photo management</p>
                            <div class="interface-methods">
                                <code>void Run(object o, EventArgs e)</code><br>
                                <code>string Name { get; }</code><br>
                                <code>string MenuLabel { get; }</code>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="detail-section">
                    <h3>📦 Plugin Loading</h3>
                    <div class="loading-process">
                        <div class="process-step">
                            <strong>1. Discovery</strong>
                            <p>Mono.Addins scans plugin directories for .addin files</p>
                        </div>
                        <div class="process-step">
                            <strong>2. Registration</strong>
                            <p>Plugin metadata is registered with the AddinManager</p>
                        </div>
                        <div class="process-step">
                            <strong>3. Activation</strong>
                            <p>Plugins are loaded on-demand when needed</p>
                        </div>
                        <div class="process-step">
                            <strong>4. Integration</strong>
                            <p>Extension points are populated with plugin implementations</p>
                        </div>
                    </div>
                </div>
            </div>

            <div class="plugin-analysis">
                <div class="analysis-card">
                    <h3>✅ Plugin System Strengths</h3>
                    <ul>
                        <li>Well-defined extension points with clear interfaces</li>
                        <li>Mature Mono.Addins framework provides robust loading</li>
                        <li>Good separation between core and plugin functionality</li>
                        <li>Comprehensive plugin coverage across major features</li>
                        <li>Plugin discovery and metadata management</li>
                    </ul>
                </div>
                <div class="analysis-card">
                    <h3>🔄 Modernization Opportunities</h3>
                    <ul>
                        <li>Migrate to modern .NET dependency injection</li>
                        <li>Add async support for plugin operations</li>
                        <li>Implement plugin sandboxing and security</li>
                        <li>Add hot-reload capabilities for development</li>
                        <li>Create modern plugin development templates</li>
                    </ul>
                </div>
                <div class="analysis-card">
                    <h3>⚠️ Current Issues</h3>
                    <ul>
                        <li>Mono.Addins dependency on legacy Mono runtime</li>
                        <li>Some plugins have UI framework dependencies</li>
                        <li>Limited error handling in plugin loading</li>
                        <li>No versioning or compatibility checking</li>
                        <li>Plugin development documentation outdated</li>
                    </ul>
                </div>
            </div>

            <div class="plugin-migration">
                <h3>🚀 Plugin Migration Strategy</h3>
                <div class="migration-timeline">
                    <div class="timeline-item">
                        <strong>Phase 1: Core Compatibility</strong>
                        <ul>
                            <li>Ensure plugins work with .NET 6+</li>
                            <li>Update Mono.Addins to latest version</li>
                            <li>Fix any breaking changes in interfaces</li>
                        </ul>
                    </div>
                    <div class="timeline-item">
                        <strong>Phase 2: UI Framework Update</strong>
                        <ul>
                            <li>Update plugins with GTK# dependencies</li>
                            <li>Create UI abstraction layer for plugins</li>
                            <li>Provide compatibility shims where needed</li>
                        </ul>
                    </div>
                    <div class="timeline-item">
                        <strong>Phase 3: Modernization</strong>
                        <ul>
                            <li>Add async/await support to plugin interfaces</li>
                            <li>Implement modern dependency injection</li>
                            <li>Add plugin security and sandboxing</li>
                        </ul>
                    </div>
                </div>
            </div>
        `;

        section.innerHTML = pluginsHTML;
    }

    // Enhanced metric tracking
    trackSectionEngagement(sectionId) {
        const engagement = JSON.parse(localStorage.getItem('fspot-engagement') || '{}');
        engagement[sectionId] = (engagement[sectionId] || 0) + 1;
        engagement[`${sectionId}_lastVisited`] = new Date().toISOString();
        localStorage.setItem('fspot-engagement', JSON.stringify(engagement));
    }
}

// Initialize the app when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.app = new FSpotDocApp();
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FSpotDocApp;
}
