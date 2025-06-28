/**
 * Real-time Content Loader for F-Spot Documentation
 * Dynamically loads and parses markdown files from CC_Docs directories
 */

class RealtimeContentLoader {
    constructor() {
        this.cache = new Map();
        this.baseUrl = '../'; // Relative to web directory
        this.contentStructure = {
            'Architecture': [
                'Class-Dependencies.md',
                'Code-Analysis.md',
                'Core-Backend.md',
                'Data-Flow.md',
                'Domain-Model.md',
                'Service-Layer.md',
                'UI-Components.md'
            ],
            'Build-System': [
                'Build-Analysis.md',
                'Dependencies.md'
            ],
            'Database': [
                'Database-Analysis.md'
            ],
            'Performance': [
                'Performance-Considerations.md'
            ],
            'Plugins': [
                'Plugin-System.md'
            ],
            'Revival-Strategy': [
                'Revival-Strategy.md'
            ],
            'Security': [
                'Security-Analysis.md'
            ],
            'UI-Framework': [
                'UI-Framework-Analysis.md'
            ]
        };
    }

    /**
     * Load and parse a markdown file
     */
    async loadMarkdownFile(category, filename) {
        const cacheKey = `${category}/${filename}`;

        if (this.cache.has(cacheKey)) {
            return this.cache.get(cacheKey);
        }

        try {
            const response = await fetch(`${this.baseUrl}${category}/${filename}`);
            if (!response.ok) {
                throw new Error(`Failed to load ${filename}: ${response.status}`);
            }

            const markdown = await response.text();
            const parsed = this.parseMarkdown(markdown);

            this.cache.set(cacheKey, parsed);
            return parsed;
        } catch (error) {
            console.error(`Error loading ${cacheKey}:`, error);
            return {
                title: filename.replace('.md', ''),
                content: `<p class="error">Failed to load content from ${filename}</p>`,
                sections: [],
                metadata: {}
            };
        }
    }

    /**
     * Parse markdown content into structured data
     */
    parseMarkdown(markdown) {
        const lines = markdown.split('\n');
        let title = '';
        let content = '';
        const sections = [];
        let currentSection = null;
        let metadata = {};

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];

            // Extract title (first h1)
            if (line.startsWith('# ') && !title) {
                title = line.substring(2).trim();
                continue;
            }

            // Extract sections (h2, h3)
            if (line.startsWith('## ')) {
                if (currentSection) {
                    sections.push(currentSection);
                }
                currentSection = {
                    title: line.substring(3).trim(),
                    content: '',
                    level: 2,
                    id: this.generateId(line.substring(3).trim())
                };
                continue;
            }

            if (line.startsWith('### ')) {
                if (currentSection) {
                    sections.push(currentSection);
                }
                currentSection = {
                    title: line.substring(4).trim(),
                    content: '',
                    level: 3,
                    id: this.generateId(line.substring(4).trim())
                };
                continue;
            }

            // Add content to current section or main content
            if (currentSection) {
                currentSection.content += line + '\n';
            } else {
                content += line + '\n';
            }
        }

        // Add final section
        if (currentSection) {
            sections.push(currentSection);
        }

        return {
            title: title || 'Untitled',
            content: this.markdownToHtml(content),
            sections: sections.map(section => ({
                ...section,
                content: this.markdownToHtml(section.content)
            })),
            metadata
        };
    }

    /**
     * Convert markdown to HTML (basic implementation)
     */
    markdownToHtml(markdown) {
        return markdown
            // Code blocks
            .replace(/```(\w+)?\n([\s\S]*?)```/g, '<pre><code class="language-$1">$2</code></pre>')
            // Inline code
            .replace(/`([^`]+)`/g, '<code>$1</code>')
            // Bold
            .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
            // Italic
            .replace(/\*([^*]+)\*/g, '<em>$1</em>')
            // Links
            .replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2">$1</a>')
            // Lists
            .replace(/^\- (.+)$/gm, '<li>$1</li>')
            .replace(/(<li>.*<\/li>)/s, '<ul>$1</ul>')
            // Paragraphs
            .replace(/\n\n/g, '</p><p>')
            .replace(/^/, '<p>')
            .replace(/$/, '</p>')
            // Clean up
            .replace(/<p><\/p>/g, '')
            .replace(/<p>(<ul>)/g, '$1')
            .replace(/(<\/ul>)<\/p>/g, '$1');
    }

    /**
     * Generate URL-friendly ID from text
     */
    generateId(text) {
        return text.toLowerCase()
            .replace(/[^a-z0-9\s-]/g, '')
            .replace(/\s+/g, '-')
            .replace(/-+/g, '-')
            .trim();
    }

    /**
     * Load all content for a category
     */
    async loadCategoryContent(category) {
        const files = this.contentStructure[category] || [];
        const content = [];

        for (const filename of files) {
            const parsed = await this.loadMarkdownFile(category, filename);
            content.push({
                filename,
                ...parsed
            });
        }

        return content;
    }

    /**
     * Search across all loaded content
     */
    async searchContent(query) {
        const results = [];
        const searchTerms = query.toLowerCase().split(' ');

        for (const [category, files] of Object.entries(this.contentStructure)) {
            for (const filename of files) {
                const content = await this.loadMarkdownFile(category, filename);

                // Search in title
                if (content.title.toLowerCase().includes(query.toLowerCase())) {
                    results.push({
                        category,
                        filename,
                        title: content.title,
                        type: 'title',
                        snippet: content.title,
                        relevance: 1.0
                    });
                }

                // Search in sections
                for (const section of content.sections) {
                    const titleMatch = searchTerms.some(term =>
                        section.title.toLowerCase().includes(term)
                    );
                    const contentMatch = searchTerms.some(term =>
                        section.content.toLowerCase().includes(term)
                    );

                    if (titleMatch || contentMatch) {
                        const snippet = this.extractSnippet(section.content, searchTerms);
                        results.push({
                            category,
                            filename,
                            title: `${content.title} - ${section.title}`,
                            type: 'section',
                            snippet,
                            sectionId: section.id,
                            relevance: titleMatch ? 0.8 : 0.5
                        });
                    }
                }
            }
        }

        return results.sort((a, b) => b.relevance - a.relevance);
    }

    /**
     * Extract relevant snippet from content
     */
    extractSnippet(content, searchTerms) {
        const text = content.replace(/<[^>]*>/g, ''); // Strip HTML
        const words = text.split(' ');

        for (let i = 0; i < words.length; i++) {
            const word = words[i].toLowerCase();
            if (searchTerms.some(term => word.includes(term))) {
                const start = Math.max(0, i - 10);
                const end = Math.min(words.length, i + 10);
                return words.slice(start, end).join(' ') + '...';
            }
        }

        return text.substring(0, 150) + '...';
    }

    /**
     * Get table of contents for all documents
     */
    async getTableOfContents() {
        const toc = {};

        for (const [category, files] of Object.entries(this.contentStructure)) {
            toc[category] = [];

            for (const filename of files) {
                const content = await this.loadMarkdownFile(category, filename);
                toc[category].push({
                    filename,
                    title: content.title,
                    sections: content.sections.map(section => ({
                        title: section.title,
                        id: section.id,
                        level: section.level
                    }))
                });
            }
        }

        return toc;
    }

    /**
     * Clear cache (useful for development)
     */
    clearCache() {
        this.cache.clear();
    }
}

// Export for use in other modules
window.RealtimeContentLoader = RealtimeContentLoader;
