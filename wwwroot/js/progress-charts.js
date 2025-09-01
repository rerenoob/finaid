// Progress Charts JavaScript functionality
// This file provides Chart.js integration and progress visualization utilities

window.ProgressCharts = {
    // Initialize a progress chart
    initializeChart: function(canvasId, data, options = {}) {
        try {
            const canvas = document.getElementById(canvasId);
            if (!canvas) {
                console.warn(`Canvas element with ID '${canvasId}' not found`);
                return null;
            }

            const ctx = canvas.getContext('2d');
            
            // Default configuration for progress charts
            const defaultOptions = {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        titleColor: '#fff',
                        bodyColor: '#fff',
                        borderColor: 'rgba(255, 255, 255, 0.1)',
                        borderWidth: 1,
                        cornerRadius: 8,
                        callbacks: {
                            label: function(context) {
                                return `Progress: ${context.parsed}%`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#6c757d'
                        }
                    },
                    y: {
                        beginAtZero: true,
                        max: 100,
                        grid: {
                            color: 'rgba(0, 0, 0, 0.1)'
                        },
                        ticks: {
                            color: '#6c757d',
                            callback: function(value) {
                                return value + '%';
                            }
                        }
                    }
                },
                animation: {
                    duration: 1500,
                    easing: 'easeOutCubic'
                }
            };

            // Merge custom options with defaults
            const finalOptions = this.mergeOptions(defaultOptions, options);

            // Create the chart
            return new Chart(ctx, {
                type: data.type || 'bar',
                data: data,
                options: finalOptions
            });
        } catch (error) {
            console.error('Error initializing chart:', error);
            return null;
        }
    },

    // Create a doughnut chart for single application progress
    createProgressDoughnut: function(canvasId, progress, title = '') {
        const data = {
            type: 'doughnut',
            data: {
                labels: ['Completed', 'Remaining'],
                datasets: [{
                    data: [progress, 100 - progress],
                    backgroundColor: [
                        this.getProgressColor(progress),
                        '#e9ecef'
                    ],
                    borderWidth: 0,
                    cutout: '70%'
                }]
            }
        };

        const options = {
            plugins: {
                tooltip: {
                    enabled: false
                },
                legend: {
                    display: false
                }
            },
            animation: {
                animateRotate: true,
                duration: 2000
            }
        };

        return this.initializeChart(canvasId, data, options);
    },

    // Create a bar chart for multiple applications
    createApplicationsBar: function(canvasId, applications) {
        const data = {
            type: 'bar',
            data: {
                labels: applications.map(app => app.name),
                datasets: [{
                    label: 'Progress',
                    data: applications.map(app => app.progress),
                    backgroundColor: applications.map(app => this.getProgressColor(app.progress)),
                    borderRadius: 6,
                    borderSkipped: false
                }]
            }
        };

        const options = {
            indexAxis: 'y', // Horizontal bar chart
            plugins: {
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const app = applications[context.dataIndex];
                            return [
                                `Progress: ${context.parsed.x}%`,
                                `Status: ${app.status}`
                            ];
                        }
                    }
                }
            },
            scales: {
                x: {
                    beginAtZero: true,
                    max: 100
                }
            }
        };

        return this.initializeChart(canvasId, data, options);
    },

    // Create a line chart for progress over time
    createProgressTimeline: function(canvasId, timelineData) {
        const data = {
            type: 'line',
            data: {
                labels: timelineData.dates,
                datasets: [{
                    label: 'Overall Progress',
                    data: timelineData.progress,
                    borderColor: '#0d6efd',
                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    tension: 0.4,
                    fill: true,
                    pointBackgroundColor: '#0d6efd',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 6,
                    pointHoverRadius: 8
                }]
            }
        };

        const options = {
            scales: {
                x: {
                    type: 'time',
                    time: {
                        unit: 'day'
                    }
                }
            },
            interaction: {
                intersect: false,
                mode: 'index'
            }
        };

        return this.initializeChart(canvasId, data, options);
    },

    // Animate progress bars
    animateProgressBar: function(elementId, targetProgress, duration = 1500) {
        const element = document.getElementById(elementId);
        if (!element) return;

        const progressBar = element.querySelector('.progress-bar');
        if (!progressBar) return;

        let currentProgress = 0;
        const increment = targetProgress / (duration / 16); // 60 FPS

        const animate = () => {
            currentProgress += increment;
            if (currentProgress >= targetProgress) {
                currentProgress = targetProgress;
                progressBar.style.width = `${currentProgress}%`;
                progressBar.setAttribute('aria-valuenow', currentProgress);
                
                // Update percentage text if exists
                const percentageText = progressBar.querySelector('.progress-text');
                if (percentageText) {
                    percentageText.textContent = `${Math.round(currentProgress)}%`;
                }
                return;
            }

            progressBar.style.width = `${currentProgress}%`;
            progressBar.setAttribute('aria-valuenow', currentProgress);
            
            // Update percentage text if exists
            const percentageText = progressBar.querySelector('.progress-text');
            if (percentageText) {
                percentageText.textContent = `${Math.round(currentProgress)}%`;
            }

            requestAnimationFrame(animate);
        };

        requestAnimationFrame(animate);
    },

    // Get color based on progress value
    getProgressColor: function(progress) {
        if (progress < 25) return '#dc3545'; // danger
        if (progress < 50) return '#fd7e14'; // warning-orange  
        if (progress < 75) return '#ffc107'; // warning
        return '#198754'; // success
    },

    // Update chart data dynamically
    updateChart: function(chart, newData) {
        if (!chart) return;

        chart.data.datasets[0].data = newData;
        chart.update('active');
    },

    // Destroy a chart instance
    destroyChart: function(chart) {
        if (chart) {
            chart.destroy();
        }
    },

    // Utility function to merge options objects
    mergeOptions: function(defaults, custom) {
        const result = { ...defaults };
        
        for (const key in custom) {
            if (custom.hasOwnProperty(key)) {
                if (typeof custom[key] === 'object' && custom[key] !== null && !Array.isArray(custom[key])) {
                    result[key] = this.mergeOptions(defaults[key] || {}, custom[key]);
                } else {
                    result[key] = custom[key];
                }
            }
        }
        
        return result;
    },

    // Initialize all progress animations on page load
    initializePageProgressAnimations: function() {
        // Animate all progress bars on the page
        const progressBars = document.querySelectorAll('[data-progress]');
        progressBars.forEach((bar, index) => {
            const progress = parseFloat(bar.getAttribute('data-progress'));
            setTimeout(() => {
                this.animateProgressBar(bar.id, progress);
            }, index * 200); // Stagger animations
        });

        // Initialize circular progress indicators
        const circularProgress = document.querySelectorAll('.circular-progress');
        circularProgress.forEach((circle, index) => {
            const progress = parseFloat(circle.getAttribute('data-progress'));
            setTimeout(() => {
                this.animateCircularProgress(circle, progress);
            }, index * 300);
        });
    },

    // Animate circular progress indicators
    animateCircularProgress: function(element, targetProgress, duration = 2000) {
        const circle = element.querySelector('.progress-ring-fill');
        if (!circle) return;

        const radius = circle.r.baseVal.value;
        const circumference = radius * 2 * Math.PI;
        const offset = circumference - (targetProgress / 100) * circumference;

        circle.style.strokeDasharray = `${circumference} ${circumference}`;
        circle.style.strokeDashoffset = circumference;

        // Animate to target
        setTimeout(() => {
            circle.style.transition = `stroke-dashoffset ${duration}ms ease-in-out`;
            circle.style.strokeDashoffset = offset;
        }, 100);
    }
};

// Initialize on DOM content loaded
document.addEventListener('DOMContentLoaded', function() {
    // Check if Chart.js is available
    if (typeof Chart === 'undefined') {
        console.warn('Chart.js is not loaded. Progress charts will not be available.');
        return;
    }

    // Initialize progress animations
    window.ProgressCharts.initializePageProgressAnimations();
});

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = window.ProgressCharts;
}