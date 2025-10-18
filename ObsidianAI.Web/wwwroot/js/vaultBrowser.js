window.vaultBrowser = {
    dotNetHelper: null,
    
    startResize: function (dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        
        // Add document-level event listeners
        document.addEventListener('mousemove', this.handleMouseMove);
        document.addEventListener('mouseup', this.handleMouseUp);
        
        // Prevent text selection during resize
        document.body.style.userSelect = 'none';
        document.body.style.cursor = 'ew-resize';
        
        console.log('Vault browser resize started');
    },
    
    handleMouseMove: function (e) {
        if (window.vaultBrowser.dotNetHelper) {
            window.vaultBrowser.dotNetHelper.invokeMethodAsync('HandleBrowserResize', e.clientX);
        }
    },
    
    handleMouseUp: function (e) {
        if (window.vaultBrowser.dotNetHelper) {
            window.vaultBrowser.dotNetHelper.invokeMethodAsync('EndBrowserResize');
        }
        
        // Remove document-level event listeners
        document.removeEventListener('mousemove', window.vaultBrowser.handleMouseMove);
        document.removeEventListener('mouseup', window.vaultBrowser.handleMouseUp);
        
        // Restore normal cursor and text selection
        document.body.style.userSelect = '';
        document.body.style.cursor = '';
        
        window.vaultBrowser.dotNetHelper = null;
        console.log('Vault browser resize ended');
    }
};
