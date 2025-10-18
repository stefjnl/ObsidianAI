window.scrollToBottom = (element) => {
    const target = typeof element === 'string'
        ? document.querySelector(element)
        : element || document.querySelector('.chat-area');

    if (!target) {
        return;
    }

    target.scrollTop = target.scrollHeight;
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

window.downloadFile = (fileName, contentType, content) => {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};