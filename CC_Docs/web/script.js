// Navigation functionality
document.addEventListener('DOMContentLoaded', function() {
    console.log('F-Spot docs loaded');
    
    const navLinks = document.querySelectorAll('.nav-link');
    const sections = document.querySelectorAll('.section');
    
    console.log('Found', navLinks.length, 'nav links and', sections.length, 'sections');
    
    // Handle navigation clicks
    navLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            // Get target section
            const targetId = this.getAttribute('href').substring(1);
            const targetSection = document.getElementById(targetId);
            
            console.log('Navigating to:', targetId);
            
            if (targetSection) {
                // Update active nav link
                navLinks.forEach(navLink => navLink.classList.remove('active'));
                this.classList.add('active');
                
                // Show target section
                sections.forEach(section => section.classList.remove('active'));
                targetSection.classList.add('active');
                
                console.log('Switched to section:', targetId);
                
                // Scroll to top of content
                const content = document.querySelector('.content');
                if (content) {
                    content.scrollTop = 0;
                }
            } else {
                console.error('Section not found:', targetId);
            }
        });
    });
    
    // Handle responsive menu toggle
    const menuToggle = document.createElement('button');
    menuToggle.className = 'menu-toggle';
    menuToggle.innerHTML = '☰';
    menuToggle.style.cssText = `
        position: fixed;
        top: 1rem;
        left: 1rem;
        z-index: 1000;
        background: var(--primary-color);
        color: white;
        border: none;
        padding: 0.5rem;
        border-radius: 4px;
        display: none;
        cursor: pointer;
        font-size: 1.25rem;
    `;
    
    document.body.appendChild(menuToggle);
    
    // Show menu toggle on mobile
    function checkScreenSize() {
        if (window.innerWidth <= 768) {
            menuToggle.style.display = 'block';
        } else {
            menuToggle.style.display = 'none';
            document.querySelector('.sidebar').classList.remove('open');
        }
    }
    
    checkScreenSize();
    window.addEventListener('resize', checkScreenSize);
    
    // Toggle sidebar on mobile
    menuToggle.addEventListener('click', function() {
        document.querySelector('.sidebar').classList.toggle('open');
    });
    
    // Close sidebar when clicking outside on mobile
    document.addEventListener('click', function(e) {
        if (window.innerWidth <= 768 && 
            !e.target.closest('.sidebar') && 
            !e.target.closest('.menu-toggle')) {
            document.querySelector('.sidebar').classList.remove('open');
        }
    });
    
    // Smooth scrolling for better UX
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
    
    // Add hover effects to interactive elements
    const interactiveElements = document.querySelectorAll('.card, .service-card, .component-card, .plugin-category, .flow-card, .phase');
    
    interactiveElements.forEach(element => {
        element.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-2px)';
        });
        
        element.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0)';
        });
    });
    
    // Initialize tooltips for technical terms
    const tooltips = {
        'God Class': 'A class that knows too much or does too much, violating the Single Responsibility Principle',
        'Repository Pattern': 'A design pattern that encapsulates data access logic and provides a more object-oriented view of the persistence layer',
        'Dependency Injection': 'A design pattern that implements IoC (Inversion of Control) for resolving dependencies',
        'Mono.Addins': 'A framework for creating extensible applications and add-ins',
        'GTK#': 'A .NET binding for the GTK+ GUI toolkit',
        'Cairo': 'A 2D graphics library with support for multiple output devices'
    };
    
    // Add tooltips to technical terms - DISABLED to prevent breaking event listeners
    // Object.keys(tooltips).forEach(term => {
    //     const regex = new RegExp(`\\b${term}\\b`, 'gi');
    //     document.body.innerHTML = document.body.innerHTML.replace(regex, 
    //         `<span class="tooltip" title="${tooltips[term]}">${term}</span>`
    //     );
    // });
    
    // Add search functionality
    const searchInput = document.createElement('input');
    searchInput.type = 'text';
    searchInput.placeholder = 'Search documentation...';
    searchInput.className = 'search-input';
    searchInput.style.cssText = `
        width: 100%;
        padding: 0.75rem;
        margin: 1rem 0;
        border: 1px solid var(--border-color);
        border-radius: 8px;
        font-size: 0.875rem;
        background: var(--bg-secondary);
    `;
    
    document.querySelector('.nav-header').appendChild(searchInput);
    
    searchInput.addEventListener('input', function() {
        const searchTerm = this.value.toLowerCase();
        const allText = document.querySelectorAll('.section p, .section li, .section h1, .section h2, .section h3');
        
        allText.forEach(element => {
            const text = element.textContent.toLowerCase();
            if (searchTerm && text.includes(searchTerm)) {
                element.style.backgroundColor = '#fef3c7';
            } else {
                element.style.backgroundColor = '';
            }
        });
    });
    
    // Add print-friendly styles
    const printStyles = `
        @media print {
            .sidebar { display: none; }
            .content { margin-left: 0; max-width: 100%; }
            .section { display: block !important; page-break-after: always; }
            .card, .service-card, .component-card { break-inside: avoid; }
        }
    `;
    
    const styleSheet = document.createElement('style');
    styleSheet.textContent = printStyles;
    document.head.appendChild(styleSheet);
    
    // Add keyboard navigation
    document.addEventListener('keydown', function(e) {
        if (e.altKey) {
            const currentActive = document.querySelector('.nav-link.active');
            const allLinks = Array.from(navLinks);
            const currentIndex = allLinks.indexOf(currentActive);
            
            if (e.key === 'ArrowDown' && currentIndex < allLinks.length - 1) {
                e.preventDefault();
                allLinks[currentIndex + 1].click();
            } else if (e.key === 'ArrowUp' && currentIndex > 0) {
                e.preventDefault();
                allLinks[currentIndex - 1].click();
            }
        }
    });
    
    console.log('F-Spot Architecture Documentation loaded successfully!');
});