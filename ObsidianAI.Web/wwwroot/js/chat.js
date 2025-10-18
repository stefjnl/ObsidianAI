// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    console.log('chat.js loaded');
    console.log('hljs available:', typeof hljs !== 'undefined');
    
    // Auto-enhance any existing code blocks
    const existingCodeBlocks = document.querySelectorAll('pre code');
    console.log('Found', existingCodeBlocks.length, 'code blocks on page load');
    
    // Set up mutation observer to catch dynamically added code blocks
    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            mutation.addedNodes.forEach((node) => {
                if (node.nodeType === 1) { // Element node
                    // Check if node itself is a markdown-content div
                    if (node.classList && node.classList.contains('markdown-content')) {
                        console.log('Markdown content added, enhancing code blocks');
                        window.highlightCode(node);
                    }
                    // Or check for code blocks within the added node
                    const codeBlocks = node.querySelectorAll && node.querySelectorAll('pre code');
                    if (codeBlocks && codeBlocks.length > 0) {
                        console.log('Found', codeBlocks.length, 'new code blocks via observer');
                        window.highlightCode(node);
                    }
                }
            });
        });
    });
    
    // Observe the chat area for changes
    const chatArea = document.querySelector('.chat-area');
    if (chatArea) {
        observer.observe(chatArea, {
            childList: true,
            subtree: true
        });
        console.log('Mutation observer set up on chat-area');
    } else {
        console.warn('Could not find .chat-area element');
    }
});

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
    // Small delay to ensure DOM is ready
    setTimeout(() => {
        if (element) {
            const codeBlocks = element.querySelectorAll('pre code');
            console.log('highlightCode: Found', codeBlocks.length, 'code blocks');
            
            codeBlocks.forEach((block) => {
                // Apply syntax highlighting
                if (typeof hljs !== 'undefined') {
                    hljs.highlightElement(block);
                }
                
                // Add copy button and language label if not already added
                const pre = block.parentElement;
                if (pre && !pre.querySelector('.code-header')) {
                    console.log('Enhancing code block');
                    enhanceCodeBlock(pre, block);
                }
            });
        }
    }, 50);
};

function enhanceCodeBlock(pre, codeBlock) {
    console.log('enhanceCodeBlock called', pre, codeBlock);
    
    // Detect language from class name (e.g., 'language-javascript')
    let language = 'text';
    const classMatch = codeBlock.className.match(/language-(\\w+)/);
    if (classMatch) {
        language = classMatch[1];
    } else if (codeBlock.classList.length > 0) {
        // Highlight.js detected language
        const detectedLang = Array.from(codeBlock.classList).find(cls => 
            cls !== 'hljs' && !cls.startsWith('language-')
        );
        if (detectedLang) {
            language = detectedLang;
        }
    }
    
    console.log('Detected language:', language);

    // Create header container
    const header = document.createElement('div');
    header.className = 'code-header';

    // Create language label
    const langLabel = document.createElement('span');
    langLabel.className = 'code-language';
    langLabel.textContent = language;

    // Create copy button
    const copyBtn = document.createElement('button');
    copyBtn.className = 'code-copy-btn';
    copyBtn.innerHTML = `
        <svg class="copy-icon" width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M5.75 4.75H10.25V1.75H5.75V4.75ZM4.5 1.75C4.5 1.05964 5.05964 0.5 5.75 0.5H10.25C10.9404 0.5 11.5 1.05964 11.5 1.75V4.75H13.25C13.9404 4.75 14.5 5.30964 14.5 6V13.25C14.5 13.9404 13.9404 14.5 13.25 14.5H2.75C2.05964 14.5 1.5 13.9404 1.5 13.25V6C1.5 5.30964 2.05964 4.75 2.75 4.75H4.5V1.75Z" fill="currentColor"/>
        </svg>
        <span class="copy-text">Copy</span>
        <svg class="check-icon" width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg" style="display: none;">
            <path d="M13.78 4.22a.75.75 0 010 1.06l-7.25 7.25a.75.75 0 01-1.06 0L2.22 9.28a.75.75 0 011.06-1.06L6 10.94l6.72-6.72a.75.75 0 011.06 0z" fill="currentColor"/>
        </svg>
    `;
    copyBtn.title = 'Copy code';

    copyBtn.addEventListener('click', async () => {
        const code = codeBlock.textContent;
        try {
            await navigator.clipboard.writeText(code);
            
            // Show success state
            const copyIcon = copyBtn.querySelector('.copy-icon');
            const checkIcon = copyBtn.querySelector('.check-icon');
            const copyText = copyBtn.querySelector('.copy-text');
            
            copyIcon.style.display = 'none';
            checkIcon.style.display = 'block';
            copyText.textContent = 'Copied!';
            copyBtn.classList.add('copied');

            // Reset after 2 seconds
            setTimeout(() => {
                copyIcon.style.display = 'block';
                checkIcon.style.display = 'none';
                copyText.textContent = 'Copy';
                copyBtn.classList.remove('copied');
            }, 2000);
        } catch (err) {
            console.error('Failed to copy:', err);
        }
    });

    header.appendChild(langLabel);
    header.appendChild(copyBtn);
    
    // Wrap pre content with header
    pre.style.position = 'relative';
    pre.insertBefore(header, pre.firstChild);
}

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

window.triggerAttachmentFileInputClick = () => {
    const fileInput = document.getElementById('attachment-file-input');
    if (fileInput) {
        fileInput.click();
    }
};