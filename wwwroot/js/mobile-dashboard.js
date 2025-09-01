// Mobile Dashboard JavaScript functionality
// Handles touch gestures, swipe navigation, and mobile-specific interactions

window.MobileDashboard = {
    // Current swipe container reference
    currentContainer: null,
    
    // Touch state
    touchState: {
        startX: 0,
        startY: 0,
        currentX: 0,
        currentY: 0,
        isDragging: false,
        startTime: 0,
        startIndex: 0
    },

    // Configuration
    config: {
        swipeThreshold: 50,
        velocityThreshold: 0.5,
        animationDuration: 300,
        dampingFactor: 0.3
    },

    // Initialize mobile dashboard
    initialize: function() {
        this.setupViewportHandler();
        this.setupPullToRefresh();
        this.setupTouchOptimizations();
        this.setupOrientationChange();
        
        console.log('Mobile Dashboard initialized');
    },

    // Setup viewport change handling
    setupViewportHandler: function() {
        const handleViewportChange = () => {
            const vh = window.innerHeight * 0.01;
            document.documentElement.style.setProperty('--vh', `${vh}px`);
            
            // Update mobile dashboard layout if needed
            this.updateLayoutForViewport();
        };

        window.addEventListener('resize', handleViewportChange);
        window.addEventListener('orientationchange', () => {
            setTimeout(handleViewportChange, 100);
        });

        // Initial setup
        handleViewportChange();
    },

    // Setup pull-to-refresh functionality
    setupPullToRefresh: function() {
        let startY = 0;
        let isPulling = false;
        let pullDistance = 0;
        const maxPullDistance = 120;
        const refreshTrigger = 80;

        const dashboardContainer = document.querySelector('.mobile-dashboard-layout');
        if (!dashboardContainer) return;

        const handlePullStart = (e) => {
            if (dashboardContainer.scrollTop > 0) return;
            
            startY = e.type.includes('touch') ? e.touches[0].clientY : e.clientY;
            isPulling = true;
        };

        const handlePullMove = (e) => {
            if (!isPulling) return;
            
            const currentY = e.type.includes('touch') ? e.touches[0].clientY : e.clientY;
            pullDistance = Math.max(0, Math.min(currentY - startY, maxPullDistance));
            
            if (pullDistance > 0) {
                e.preventDefault();
                
                const pullIndicator = this.getPullToRefreshIndicator();
                if (pullIndicator) {
                    pullIndicator.style.transform = `translateY(${pullDistance}px)`;
                    pullIndicator.style.opacity = Math.min(pullDistance / refreshTrigger, 1);
                    
                    if (pullDistance >= refreshTrigger) {
                        pullIndicator.classList.add('ready-to-refresh');
                    } else {
                        pullIndicator.classList.remove('ready-to-refresh');
                    }
                }
                
                dashboardContainer.style.transform = `translateY(${pullDistance * 0.5}px)`;
            }
        };

        const handlePullEnd = (e) => {
            if (!isPulling) return;
            
            isPulling = false;
            const shouldRefresh = pullDistance >= refreshTrigger;
            
            // Reset visual state
            const pullIndicator = this.getPullToRefreshIndicator();
            if (pullIndicator) {
                pullIndicator.style.transform = '';
                pullIndicator.style.opacity = '';
                pullIndicator.classList.remove('ready-to-refresh');
            }
            
            dashboardContainer.style.transform = '';
            
            if (shouldRefresh) {
                this.triggerRefresh();
            }
            
            pullDistance = 0;
        };

        // Add event listeners
        dashboardContainer.addEventListener('touchstart', handlePullStart, { passive: false });
        dashboardContainer.addEventListener('touchmove', handlePullMove, { passive: false });
        dashboardContainer.addEventListener('touchend', handlePullEnd, { passive: false });
    },

    // Get or create pull-to-refresh indicator
    getPullToRefreshIndicator: function() {
        let indicator = document.querySelector('.pull-to-refresh-indicator');
        
        if (!indicator) {
            indicator = document.createElement('div');
            indicator.className = 'pull-to-refresh-indicator';
            indicator.innerHTML = `
                <div class="refresh-icon">
                    <i class="bi bi-arrow-down-circle"></i>
                </div>
                <span class="refresh-text">Pull to refresh</span>
            `;
            
            const dashboardContainer = document.querySelector('.mobile-dashboard-layout');
            if (dashboardContainer) {
                dashboardContainer.insertBefore(indicator, dashboardContainer.firstChild);
            }
        }
        
        return indicator;
    },

    // Trigger dashboard refresh
    triggerRefresh: function() {
        // Find and trigger the refresh button if available
        const refreshBtn = document.querySelector('.bottom-action-btn');
        if (refreshBtn && !refreshBtn.disabled) {
            refreshBtn.click();
        }
        
        // Or trigger via .NET interop if available
        if (window.DotNet) {
            try {
                DotNet.invokeMethodAsync('finaid', 'RefreshDashboard');
            } catch (error) {
                console.log('Could not trigger .NET refresh:', error.message);
            }
        }
    },

    // Setup touch optimizations
    setupTouchOptimizations: function() {
        // Improve tap responsiveness
        document.addEventListener('touchstart', function() {}, { passive: true });
        
        // Prevent zoom on double tap for dashboard elements
        const dashboardElements = document.querySelectorAll('.mobile-card, .mobile-action-item, .btn');
        dashboardElements.forEach(element => {
            element.addEventListener('touchend', (e) => {
                e.preventDefault();
                element.click();
            });
        });
        
        // Add visual feedback for touches
        this.setupTouchFeedback();
    },

    // Setup visual touch feedback
    setupTouchFeedback: function() {
        const addTouchFeedback = (selector, className = 'touch-feedback') => {
            const elements = document.querySelectorAll(selector);
            elements.forEach(element => {
                element.addEventListener('touchstart', () => {
                    element.classList.add(className);
                });
                
                element.addEventListener('touchend', () => {
                    setTimeout(() => {
                        element.classList.remove(className);
                    }, 150);
                });
                
                element.addEventListener('touchcancel', () => {
                    element.classList.remove(className);
                });
            });
        };

        addTouchFeedback('.mobile-card');
        addTouchFeedback('.mobile-action-item');
        addTouchFeedback('.btn');
        addTouchFeedback('.quick-action-item');
    },

    // Setup orientation change handling
    setupOrientationChange: function() {
        const handleOrientationChange = () => {
            // Update card layouts for orientation
            const cards = document.querySelectorAll('.mobile-card');
            cards.forEach(card => {
                if (window.innerHeight < 500) {
                    card.classList.add('landscape-mode');
                } else {
                    card.classList.remove('landscape-mode');
                }
            });
            
            // Recalculate swipe container dimensions
            if (this.currentContainer) {
                this.recalculateSwipeContainer(this.currentContainer);
            }
        };

        window.addEventListener('orientationchange', () => {
            setTimeout(handleOrientationChange, 100);
        });

        // Initial setup
        handleOrientationChange();
    },

    // Update layout based on current viewport
    updateLayoutForViewport: function() {
        const viewport = {
            width: window.innerWidth,
            height: window.innerHeight,
            orientation: window.innerWidth > window.innerHeight ? 'landscape' : 'portrait'
        };

        // Update CSS custom properties
        document.documentElement.style.setProperty('--viewport-width', `${viewport.width}px`);
        document.documentElement.style.setProperty('--viewport-height', `${viewport.height}px`);

        // Apply viewport-specific classes
        document.body.classList.toggle('mobile-landscape', viewport.orientation === 'landscape' && viewport.width < 768);
        document.body.classList.toggle('mobile-portrait', viewport.orientation === 'portrait' && viewport.width < 768);

        // Emit viewport change event for components
        window.dispatchEvent(new CustomEvent('viewportChanged', { detail: viewport }));
    },

    // Initialize swipe container
    initializeSwipeContainer: function(container, options = {}) {
        if (!container) return null;

        this.currentContainer = container;
        const config = { ...this.config, ...options };
        
        let currentIndex = 0;
        const cards = container.querySelectorAll('.mobile-card');
        const totalCards = cards.length;
        
        if (totalCards === 0) return null;

        const track = container.querySelector('.cards-track') || container;
        
        const swipeController = {
            currentIndex,
            totalCards,
            container,
            track,
            config,
            
            navigateToCard: function(index, animate = true) {
                if (index < 0 || index >= this.totalCards) return false;
                
                this.currentIndex = index;
                const translateX = -index * 100;
                
                if (animate) {
                    this.track.style.transition = `transform ${this.config.animationDuration}ms cubic-bezier(0.25, 0.46, 0.45, 0.94)`;
                } else {
                    this.track.style.transition = 'none';
                }
                
                this.track.style.transform = `translateX(${translateX}%)`;
                
                // Update indicators
                this.updateIndicators();
                
                // Emit change event
                container.dispatchEvent(new CustomEvent('cardChange', { 
                    detail: { index, totalCards: this.totalCards }
                }));
                
                return true;
            },
            
            updateIndicators: function() {
                const indicators = container.querySelectorAll('.indicator');
                indicators.forEach((indicator, index) => {
                    indicator.classList.toggle('active', index === this.currentIndex);
                });
            },
            
            nextCard: function() {
                return this.navigateToCard(Math.min(this.currentIndex + 1, this.totalCards - 1));
            },
            
            previousCard: function() {
                return this.navigateToCard(Math.max(this.currentIndex - 1, 0));
            }
        };

        // Setup touch handlers
        this.setupSwipeHandlers(swipeController);
        
        return swipeController;
    },

    // Setup swipe gesture handlers
    setupSwipeHandlers: function(controller) {
        const { container, track, config } = controller;
        
        const handleStart = (e) => {
            this.touchState.isDragging = true;
            this.touchState.startX = e.type.includes('touch') ? e.touches[0].clientX : e.clientX;
            this.touchState.startY = e.type.includes('touch') ? e.touches[0].clientY : e.clientY;
            this.touchState.currentX = this.touchState.startX;
            this.touchState.currentY = this.touchState.startY;
            this.touchState.startTime = Date.now();
            this.touchState.startIndex = controller.currentIndex;
            
            track.style.transition = 'none';
            container.style.cursor = 'grabbing';
        };

        const handleMove = (e) => {
            if (!this.touchState.isDragging) return;
            
            this.touchState.currentX = e.type.includes('touch') ? e.touches[0].clientX : e.clientX;
            this.touchState.currentY = e.type.includes('touch') ? e.touches[0].clientY : e.clientY;
            
            const deltaX = this.touchState.currentX - this.touchState.startX;
            const deltaY = Math.abs(this.touchState.currentY - this.touchState.startY);
            
            // Only handle horizontal swipes
            if (Math.abs(deltaX) > deltaY && Math.abs(deltaX) > 10) {
                e.preventDefault();
                
                const currentTransform = -controller.currentIndex * 100;
                const dragPercent = (deltaX / container.offsetWidth) * 100 * config.dampingFactor;
                const newTransform = currentTransform + dragPercent;
                
                track.style.transform = `translateX(${newTransform}%)`;
            }
        };

        const handleEnd = (e) => {
            if (!this.touchState.isDragging) return;
            
            this.touchState.isDragging = false;
            
            const deltaX = this.touchState.currentX - this.touchState.startX;
            const deltaTime = Date.now() - this.touchState.startTime;
            const velocity = Math.abs(deltaX) / deltaTime;
            
            container.style.cursor = 'grab';
            track.style.transition = `transform ${config.animationDuration}ms cubic-bezier(0.25, 0.46, 0.45, 0.94)`;
            
            let targetIndex = controller.currentIndex;
            
            // Determine if swipe should trigger navigation
            if (Math.abs(deltaX) > config.swipeThreshold || velocity > config.velocityThreshold) {
                if (deltaX > 0 && controller.currentIndex > 0) {
                    targetIndex = controller.currentIndex - 1;
                } else if (deltaX < 0 && controller.currentIndex < controller.totalCards - 1) {
                    targetIndex = controller.currentIndex + 1;
                }
            }
            
            controller.navigateToCard(targetIndex);
        };

        // Add event listeners
        container.addEventListener('mousedown', handleStart);
        container.addEventListener('mousemove', handleMove);
        container.addEventListener('mouseup', handleEnd);
        container.addEventListener('mouseleave', handleEnd);

        container.addEventListener('touchstart', handleStart, { passive: false });
        container.addEventListener('touchmove', handleMove, { passive: false });
        container.addEventListener('touchend', handleEnd, { passive: false });

        // Prevent context menu
        container.addEventListener('contextmenu', e => e.preventDefault());
        
        // Set initial cursor
        container.style.cursor = 'grab';
    },

    // Recalculate swipe container dimensions
    recalculateSwipeContainer: function(container) {
        const track = container.querySelector('.cards-track');
        if (!track) return;
        
        // Force reflow to get accurate dimensions
        container.offsetHeight;
        
        // Reset transform temporarily
        const currentTransform = track.style.transform;
        track.style.transform = 'translateX(0%)';
        
        // Get card width
        const card = track.querySelector('.mobile-card');
        if (card) {
            const cardWidth = card.offsetWidth;
            track.style.transform = currentTransform;
        }
    },

    // Utility function to check if device supports touch
    isTouchDevice: function() {
        return 'ontouchstart' in window || navigator.maxTouchPoints > 0;
    },

    // Utility function to get device orientation
    getOrientation: function() {
        if (screen.orientation) {
            return screen.orientation.angle === 0 || screen.orientation.angle === 180 ? 'portrait' : 'landscape';
        }
        return window.innerWidth > window.innerHeight ? 'landscape' : 'portrait';
    },

    // Cleanup function
    cleanup: function() {
        this.currentContainer = null;
        this.touchState = {
            startX: 0,
            startY: 0,
            currentX: 0,
            currentY: 0,
            isDragging: false,
            startTime: 0,
            startIndex: 0
        };
    }
};

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    if (window.innerWidth <= 768) {
        window.MobileDashboard.initialize();
    }
});

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = window.MobileDashboard;
}