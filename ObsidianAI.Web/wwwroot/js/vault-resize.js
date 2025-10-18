/**
 * Vault Browser Resize Helper
 * Provides smooth resize functionality with performance optimizations
 */
window.VaultResizeHelper = {
    /**
     * Initialize resize functionality
     * @param {HTMLElement} handle - The resize handle element
     * @param {HTMLElement} panel - The panel to resize
     * @param {number} minWidth - Minimum width in pixels
     * @param {number} maxWidth - Maximum width in pixels
     * @param {function} onResize - Callback function when resizing
     */
    initialize: function (handle, panel, minWidth, maxWidth, onResize) {
        if (!handle || !panel) {
            console.warn('VaultResizeHelper: Invalid elements provided');
            return;
        }

        let isResizing = false;
        let startX = 0;
        let startWidth = 0;
        let animationFrameId = null;

        const updateWidth = (newWidth) => {
            const clampedWidth = Math.max(minWidth, Math.min(maxWidth, newWidth));
            panel.style.width = `${clampedWidth}px`;
            if (onResize) {
                onResize(clampedWidth);
            }
        };

        const handleMouseMove = (e) => {
            if (!isResizing) return;

            if (animationFrameId) {
                cancelAnimationFrame(animationFrameId);
            }

            animationFrameId = requestAnimationFrame(() => {
                const deltaX = e.clientX - startX;
                const newWidth = startWidth + deltaX;
                updateWidth(newWidth);
            });
        };

        const handleTouchMove = (e) => {
            if (!isResizing || e.touches.length === 0) return;

            e.preventDefault();

            if (animationFrameId) {
                cancelAnimationFrame(animationFrameId);
            }

            animationFrameId = requestAnimationFrame(() => {
                const touch = e.touches[0];
                const deltaX = touch.clientX - startX;
                const newWidth = startWidth + deltaX;
                updateWidth(newWidth);
            });
        };

        const startResize = (clientX) => {
            isResizing = true;
            startX = clientX;
            startWidth = panel.offsetWidth;
            handle.classList.add('vault-resize-handle-active');
            document.body.style.cursor = 'ew-resize';
            document.body.style.userSelect = 'none';
        };

        const endResize = () => {
            if (!isResizing) return;
            
            isResizing = false;
            handle.classList.remove('vault-resize-handle-active');
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
            
            if (animationFrameId) {
                cancelAnimationFrame(animationFrameId);
                animationFrameId = null;
            }
        };

        // Mouse events
        handle.addEventListener('mousedown', (e) => {
            e.preventDefault();
            startResize(e.clientX);
        });

        document.addEventListener('mousemove', handleMouseMove);
        document.addEventListener('mouseup', endResize);

        // Touch events
        handle.addEventListener('touchstart', (e) => {
            if (e.touches.length > 0) {
                e.preventDefault();
                startResize(e.touches[0].clientX);
            }
        }, { passive: false });

        document.addEventListener('touchmove', handleTouchMove, { passive: false });
        document.addEventListener('touchend', endResize);

        // Cleanup function
        return () => {
            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', endResize);
            document.removeEventListener('touchmove', handleTouchMove);
            document.removeEventListener('touchend', endResize);
            if (animationFrameId) {
                cancelAnimationFrame(animationFrameId);
            }
        };
    },

    /**
     * Save panel width to localStorage
     * @param {string} key - Storage key
     * @param {number} width - Width to save
     */
    saveWidth: function (key, width) {
        try {
            localStorage.setItem(key, width.toString());
        } catch (e) {
            console.warn('VaultResizeHelper: Failed to save width to localStorage', e);
        }
    },

    /**
     * Load panel width from localStorage
     * @param {string} key - Storage key
     * @param {number} defaultWidth - Default width if not found
     * @returns {number} - Loaded or default width
     */
    loadWidth: function (key, defaultWidth) {
        try {
            const saved = localStorage.getItem(key);
            return saved ? parseFloat(saved) : defaultWidth;
        } catch (e) {
            console.warn('VaultResizeHelper: Failed to load width from localStorage', e);
            return defaultWidth;
        }
    }
};
