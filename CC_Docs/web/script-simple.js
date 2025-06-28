// Simple, clean navigation functionality for F-Spot Architecture Documentation
document.addEventListener('DOMContentLoaded', function() {
    console.log('F-Spot Architecture Documentation loaded');
    
    const navLinks = document.querySelectorAll('.nav-link');
    const sections = document.querySelectorAll('.section');
    
    console.log(`Found ${navLinks.length} navigation links and ${sections.length} sections`);
    
    // Handle navigation clicks
    navLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            // Get target section ID
            const targetId = this.getAttribute('href').substring(1);
            const targetSection = document.getElementById(targetId);
            
            console.log(`Navigating to section: ${targetId}`);
            
            if (targetSection) {
                // Update active navigation link
                navLinks.forEach(navLink => navLink.classList.remove('active'));
                this.classList.add('active');
                
                // Show target section, hide all others
                sections.forEach(section => section.classList.remove('active'));
                targetSection.classList.add('active');
                
                // Scroll to top of content area
                const content = document.querySelector('.content');
                if (content) {
                    content.scrollTop = 0;
                }
                
                console.log(`Successfully switched to section: ${targetId}`);
            } else {
                console.error(`Section not found: ${targetId}`);
            }
        });
    });
    
    // Handle mobile menu toggle
    function setupMobileMenu() {
        const menuToggle = document.createElement('button');
        menuToggle.className = 'menu-toggle';
        menuToggle.innerHTML = '☰';
        menuToggle.style.cssText = `
            position: fixed;
            top: 1rem;
            left: 1rem;
            z-index: 1000;
            background: var(--primary-color, #2563eb);
            color: white;
            border: none;
            padding: 0.5rem;
            border-radius: 4px;
            display: none;
            cursor: pointer;
            font-size: 1.25rem;
        `;
        
        document.body.appendChild(menuToggle);
        
        // Show/hide menu toggle based on screen size
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
    }
    
    setupMobileMenu();
    
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
    
    console.log('F-Spot Architecture Documentation initialized successfully!');
});