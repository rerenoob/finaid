// Chat Interface JavaScript Functions

/**
 * Scroll to the bottom of a chat messages container
 * @param {string} containerId - The ID of the messages container
 */
window.scrollToBottom = (containerId) => {
    try {
        const container = document.getElementById(containerId);
        if (container) {
            // Smooth scroll to bottom
            container.scrollTo({
                top: container.scrollHeight,
                behavior: 'smooth'
            });
        }
    } catch (error) {
        console.warn('Error scrolling to bottom:', error);
    }
};

/**
 * Auto-resize textarea based on content
 * @param {HTMLElement} textarea - The textarea element
 */
window.autoResizeTextarea = (textarea) => {
    try {
        if (textarea) {
            textarea.style.height = 'auto';
            textarea.style.height = Math.min(textarea.scrollHeight, 120) + 'px';
        }
    } catch (error) {
        console.warn('Error auto-resizing textarea:', error);
    }
};

/**
 * Focus on an input element
 * @param {string} elementId - The ID of the input element
 */
window.focusElement = (elementId) => {
    try {
        const element = document.getElementById(elementId);
        if (element) {
            element.focus();
        }
    } catch (error) {
        console.warn('Error focusing element:', error);
    }
};

/**
 * Copy text to clipboard
 * @param {string} text - Text to copy
 * @returns {Promise<boolean>} True if successful
 */
window.copyToClipboard = async (text) => {
    try {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return true;
        } else {
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'fixed';
            textArea.style.left = '-999999px';
            textArea.style.top = '-999999px';
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            const result = document.execCommand('copy');
            document.body.removeChild(textArea);
            return result;
        }
    } catch (error) {
        console.error('Error copying to clipboard:', error);
        return false;
    }
};

/**
 * Play notification sound
 */
window.playNotificationSound = () => {
    try {
        // Create a simple beep sound
        const audioContext = new (window.AudioContext || window.webkitAudioContext)();
        const oscillator = audioContext.createOscillator();
        const gainNode = audioContext.createGain();
        
        oscillator.connect(gainNode);
        gainNode.connect(audioContext.destination);
        
        oscillator.frequency.value = 800;
        oscillator.type = 'sine';
        gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.1);
        
        oscillator.start(audioContext.currentTime);
        oscillator.stop(audioContext.currentTime + 0.1);
    } catch (error) {
        console.warn('Error playing notification sound:', error);
    }
};

/**
 * Show browser notification
 * @param {string} title - Notification title
 * @param {string} body - Notification body
 * @param {string} icon - Notification icon URL
 */
window.showNotification = async (title, body, icon = '/favicon.ico') => {
    try {
        if (!('Notification' in window)) {
            console.warn('This browser does not support desktop notification');
            return;
        }

        if (Notification.permission === 'granted') {
            new Notification(title, {
                body: body,
                icon: icon,
                badge: icon,
                tag: 'chat-notification',
                renotify: true,
                requireInteraction: false,
                silent: false
            });
        } else if (Notification.permission !== 'denied') {
            const permission = await Notification.requestPermission();
            if (permission === 'granted') {
                new Notification(title, {
                    body: body,
                    icon: icon,
                    badge: icon,
                    tag: 'chat-notification'
                });
            }
        }
    } catch (error) {
        console.warn('Error showing notification:', error);
    }
};

/**
 * Check if the page is visible (not in background tab)
 * @returns {boolean} True if page is visible
 */
window.isPageVisible = () => {
    return !document.hidden;
};

/**
 * Format text with basic markdown
 * @param {string} text - Raw text to format
 * @returns {string} HTML formatted text
 */
window.formatMarkdown = (text) => {
    try {
        if (!text) return '';
        
        // Escape HTML first
        text = text.replace(/&/g, '&amp;')
                  .replace(/</g, '&lt;')
                  .replace(/>/g, '&gt;')
                  .replace(/"/g, '&quot;')
                  .replace(/'/g, '&#39;');
        
        // Convert line breaks
        text = text.replace(/\n/g, '<br>');
        
        // Bold **text**
        text = text.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
        
        // Italic *text*
        text = text.replace(/(?<!\*)\*([^\*\n]+?)\*(?!\*)/g, '<em>$1</em>');
        
        // Code `text`
        text = text.replace(/`([^`\n]+?)`/g, '<code>$1</code>');
        
        // Links [text](url)
        text = text.replace(/\[([^\]]+?)\]\(([^)]+?)\)/g, 
            '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>');
        
        return text;
    } catch (error) {
        console.warn('Error formatting markdown:', error);
        return text || '';
    }
};

/**
 * Debounce function calls
 * @param {Function} func - Function to debounce
 * @param {number} wait - Wait time in milliseconds
 * @returns {Function} Debounced function
 */
window.debounce = (func, wait) => {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
};

/**
 * Throttle function calls
 * @param {Function} func - Function to throttle
 * @param {number} limit - Limit in milliseconds
 * @returns {Function} Throttled function
 */
window.throttle = (func, limit) => {
    let inThrottle;
    return function() {
        const args = arguments;
        const context = this;
        if (!inThrottle) {
            func.apply(context, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
};

/**
 * Initialize chat interface features
 */
window.initializeChatInterface = () => {
    try {
        // Add keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            // Ctrl/Cmd + K to focus chat input
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                const chatInput = document.querySelector('.message-textarea');
                if (chatInput) {
                    chatInput.focus();
                }
            }
            
            // Escape to clear chat input
            if (e.key === 'Escape') {
                const chatInput = document.querySelector('.message-textarea');
                if (chatInput && document.activeElement === chatInput) {
                    chatInput.value = '';
                    chatInput.style.height = 'auto';
                }
            }
        });
        
        // Auto-resize textareas
        document.addEventListener('input', (e) => {
            if (e.target.classList.contains('message-textarea')) {
                window.autoResizeTextarea(e.target);
            }
        });
        
        // Handle visibility changes for notifications
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                // Page is now hidden - can enable notifications
                console.log('Page hidden - notifications enabled');
            } else {
                // Page is now visible - disable notifications
                console.log('Page visible - notifications disabled');
            }
        });
        
        console.log('Chat interface initialized');
    } catch (error) {
        console.error('Error initializing chat interface:', error);
    }
};

/**
 * Cleanup chat interface
 */
window.cleanupChatInterface = () => {
    try {
        // Remove event listeners if needed
        // This would be called when the component is disposed
        console.log('Chat interface cleaned up');
    } catch (error) {
        console.error('Error cleaning up chat interface:', error);
    }
};

// Initialize when DOM is loaded
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', window.initializeChatInterface);
} else {
    window.initializeChatInterface();
}

// Export functions for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        scrollToBottom: window.scrollToBottom,
        autoResizeTextarea: window.autoResizeTextarea,
        focusElement: window.focusElement,
        copyToClipboard: window.copyToClipboard,
        playNotificationSound: window.playNotificationSound,
        showNotification: window.showNotification,
        isPageVisible: window.isPageVisible,
        formatMarkdown: window.formatMarkdown,
        debounce: window.debounce,
        throttle: window.throttle,
        initializeChatInterface: window.initializeChatInterface,
        cleanupChatInterface: window.cleanupChatInterface
    };
}