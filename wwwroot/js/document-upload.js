// Document Upload JavaScript Module
window.documentUpload = {
    dragCounter: 0,
    blazorRef: null,

    initialize: function (dotNetRef) {
        this.blazorRef = dotNetRef;
        this.setupDragAndDrop();
        this.setupFileInputTrigger();
    },

    setupDragAndDrop: function () {
        const dropzone = document.querySelector('.upload-dropzone');
        if (!dropzone) return;

        // Prevent default drag behaviors
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            document.addEventListener(eventName, this.preventDefaults, false);
            dropzone.addEventListener(eventName, this.preventDefaults, false);
        });

        // Handle drag enter/leave for the entire document to prevent flicker
        document.addEventListener('dragenter', (e) => {
            this.dragCounter++;
            if (this.isValidFile(e)) {
                dropzone.classList.add('drag-over');
            }
        });

        document.addEventListener('dragleave', (e) => {
            this.dragCounter--;
            if (this.dragCounter <= 0) {
                this.dragCounter = 0;
                dropzone.classList.remove('drag-over');
            }
        });

        document.addEventListener('drop', (e) => {
            this.dragCounter = 0;
            dropzone.classList.remove('drag-over');
        });

        // Handle drop on dropzone
        dropzone.addEventListener('drop', (e) => {
            this.handleDrop(e);
        });
    },

    setupFileInputTrigger: function () {
        // This function will be called when the dropzone is clicked
    },

    preventDefaults: function (e) {
        e.preventDefault();
        e.stopPropagation();
    },

    isValidFile: function (e) {
        // Check if dragged items contain files
        if (!e.dataTransfer || !e.dataTransfer.types) return false;
        
        return e.dataTransfer.types.includes('Files');
    },

    handleDrop: function (e) {
        const files = e.dataTransfer.files;
        
        if (files.length === 0) return;

        // Validate files before processing
        const validFiles = [];
        const allowedTypes = ['.pdf', '.jpg', '.jpeg', '.png', '.tiff', '.tif'];
        const maxSize = 50 * 1024 * 1024; // 50MB

        for (let i = 0; i < files.length; i++) {
            const file = files[i];
            const extension = this.getFileExtension(file.name).toLowerCase();
            
            if (!allowedTypes.includes(extension)) {
                this.showError(`File "${file.name}" has an unsupported file type. Please upload PDF, JPG, PNG, or TIFF files only.`);
                continue;
            }

            if (file.size > maxSize) {
                this.showError(`File "${file.name}" is too large. Maximum file size is 50MB.`);
                continue;
            }

            validFiles.push(file);
        }

        if (validFiles.length === 0) return;

        // Trigger the InputFile component
        this.simulateFileInput(validFiles);
    },

    simulateFileInput: function (files) {
        const fileInput = document.querySelector('input[type="file"][multiple]');
        if (!fileInput) return;

        // Create a new DataTransfer object to simulate file selection
        const dataTransfer = new DataTransfer();
        
        for (let i = 0; i < files.length; i++) {
            dataTransfer.items.add(files[i]);
        }

        // Set the files property of the input
        fileInput.files = dataTransfer.files;

        // Trigger the change event
        const event = new Event('change', { bubbles: true });
        fileInput.dispatchEvent(event);
    },

    triggerFileInput: function () {
        const fileInput = document.querySelector('input[type="file"][multiple]');
        if (fileInput) {
            fileInput.click();
        }
    },

    getFileExtension: function (filename) {
        return filename.slice(filename.lastIndexOf('.'));
    },

    showError: function (message) {
        // For now, use a simple alert. In a real app, you'd use a toast notification
        console.error('Upload Error:', message);
        
        // You could also call back to Blazor to show the error
        if (this.blazorRef) {
            // This would require adding a method to the Blazor component
            // this.blazorRef.invokeMethodAsync('ShowUploadError', message);
        }
    },

    // Utility function to format file sizes
    formatFileSize: function (bytes) {
        const sizes = ['B', 'KB', 'MB', 'GB'];
        if (bytes === 0) return '0 B';
        
        const i = Math.floor(Math.log(bytes) / Math.log(1024));
        return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
    },

    // Function to validate file types by checking magic numbers (file signatures)
    validateFileSignature: async function (file) {
        return new Promise((resolve) => {
            const reader = new FileReader();
            
            reader.onload = function (e) {
                const arr = new Uint8Array(e.target.result);
                const hex = Array.from(arr).map(b => b.toString(16).padStart(2, '0')).join('');
                
                // Check common file signatures
                const signatures = {
                    '25504446': 'pdf',  // PDF
                    'ffd8ff': 'jpg',    // JPEG
                    '89504e47': 'png',  // PNG
                    '49492a00': 'tiff', // TIFF (little endian)
                    '4d4d002a': 'tiff'  // TIFF (big endian)
                };
                
                let isValid = false;
                for (const [signature, type] of Object.entries(signatures)) {
                    if (hex.startsWith(signature.toLowerCase())) {
                        isValid = true;
                        break;
                    }
                }
                
                resolve(isValid);
            };
            
            reader.onerror = () => resolve(false);
            reader.readAsArrayBuffer(file.slice(0, 8)); // Read first 8 bytes
        });
    },

    // Clean up event listeners
    dispose: function () {
        const dropzone = document.querySelector('.upload-dropzone');
        if (dropzone) {
            ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
                document.removeEventListener(eventName, this.preventDefaults, false);
                dropzone.removeEventListener(eventName, this.preventDefaults, false);
            });
        }
        
        this.blazorRef = null;
        this.dragCounter = 0;
    }
};

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    // Auto-initialization will be handled by the Blazor component
    console.log('Document upload module ready');
});

// Cleanup on page unload
window.addEventListener('beforeunload', function () {
    if (window.documentUpload) {
        window.documentUpload.dispose();
    }
});