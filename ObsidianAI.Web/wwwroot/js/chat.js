window.scrollToBottom = (element) => {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

window.highlightCode = (element) => {
    if (element) {
        const codeBlocks = element.querySelectorAll('pre code');
        codeBlocks.forEach((block) => {
            if (typeof hljs !== 'undefined') {
                hljs.highlightElement(block);
            }
        });
    }
};

window.openObsidianUri = (uri) => {
    window.open(uri, '_blank');
};