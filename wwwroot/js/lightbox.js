// Lightbox functionality for QuillKit
class Lightbox {
    constructor() {
        this.overlay = null;
        this.init();
    }

    init() {
        // Create lightbox overlay HTML
        this.createOverlay();
        
        // Bind click events to all lightbox images
        this.bindEvents();
    }

    createOverlay() {
        // Create overlay container
        this.overlay = document.createElement('div');
        this.overlay.className = 'lightbox-overlay';
        this.overlay.innerHTML = `
            <div class="lightbox-content">
                <button class="lightbox-close" aria-label="Close lightbox">&times;</button>
                <img class="lightbox-image" src="" alt="">
                <div class="lightbox-navigation">
                    <button class="lightbox-prev" aria-label="Previous image">&#8249;</button>
                    <button class="lightbox-next" aria-label="Next image">&#8250;</button>
                </div>
            </div>
        `;
        
        document.body.appendChild(this.overlay);
        
        // Bind overlay events
        this.overlay.querySelector('.lightbox-close').addEventListener('click', () => this.close());
        this.overlay.addEventListener('click', (e) => {
            if (e.target === this.overlay) {
                this.close();
            }
        });
        
        // Navigation buttons
        this.overlay.querySelector('.lightbox-prev').addEventListener('click', () => this.previous());
        this.overlay.querySelector('.lightbox-next').addEventListener('click', () => this.next());
        
        // Keyboard events
        document.addEventListener('keydown', (e) => {
            if (this.overlay.classList.contains('active')) {
                switch(e.key) {
                    case 'Escape':
                        this.close();
                        break;
                    case 'ArrowLeft':
                        this.previous();
                        break;
                    case 'ArrowRight':
                        this.next();
                        break;
                }
            }
        });
    }

    bindEvents() {
        // Find all lightbox images and bind click events
        document.addEventListener('click', (e) => {
            const lightboxLink = e.target.closest('a.lightbox');
            if (lightboxLink) {
                e.preventDefault();
                this.open(lightboxLink.href, lightboxLink.getAttribute('rel'));
            }
        });
    }

    open(imageSrc, group = null) {
        const img = this.overlay.querySelector('.lightbox-image');
        img.src = imageSrc;
        
        // Store current group for navigation
        this.currentGroup = group;
        this.currentIndex = this.getCurrentIndex(imageSrc, group);
        
        // Show/hide navigation based on group
        const nav = this.overlay.querySelector('.lightbox-navigation');
        if (group && this.getGroupImages(group).length > 1) {
            nav.style.display = 'block';
        } else {
            nav.style.display = 'none';
        }
        
        this.overlay.classList.add('active');
        document.body.style.overflow = 'hidden'; // Prevent scrolling
    }

    close() {
        this.overlay.classList.remove('active');
        document.body.style.overflow = ''; // Restore scrolling
    }

    getCurrentIndex(imageSrc, group) {
        if (!group) return 0;
        const images = this.getGroupImages(group);
        return images.findIndex(img => img.href === imageSrc);
    }

    getGroupImages(group) {
        return Array.from(document.querySelectorAll(`a.lightbox[rel="${group}"]`));
    }

    previous() {
        if (!this.currentGroup) return;
        
        const images = this.getGroupImages(this.currentGroup);
        this.currentIndex = (this.currentIndex - 1 + images.length) % images.length;
        const prevImage = images[this.currentIndex];
        
        this.overlay.querySelector('.lightbox-image').src = prevImage.href;
    }

    next() {
        if (!this.currentGroup) return;
        
        const images = this.getGroupImages(this.currentGroup);
        this.currentIndex = (this.currentIndex + 1) % images.length;
        const nextImage = images[this.currentIndex];
        
        this.overlay.querySelector('.lightbox-image').src = nextImage.href;
    }
}

// Initialize lightbox when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    new Lightbox();
});
