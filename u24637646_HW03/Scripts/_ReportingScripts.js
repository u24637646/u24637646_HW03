document.addEventListener('DOMContentLoaded', function () {

    // --- DATA ACCESS ---
    const historicalLineLabels = window.lineLabels;
    const historicalLineData = window.lineData;
    const doughnutLabels = window.DoughnutLabels;
    const doughnutData = window.DoughnutData;

    // Global variables for control and state
    let chartInterval = null;
    let monthlyLineChartInstance = null;
    let dataIndex = 0; // Tracks the current position in the historical data

    // =========================================================
    // 1. LINE CHART SETUP AND CONTROL (Months appear as data runs)
    // =========================================================

    const lineConfig = {
        type: 'line',
        data: {
            labels: [],
            datasets: [{
                label: 'Total Orders per Month',
                data: [],
                backgroundColor: 'rgba(54, 162, 235, 0.7)',
                borderColor: 'rgb(54, 162, 235)',
                borderWidth: 3,
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    type: 'category',
                    // The labels array is intentionally empty here and populated during the simulation.
                },
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Number of Orders'
                    }
                }
            },
            plugins: {
                legend: { display: false },
            }
        }
    };

    // Initialize the Chart.js instance once
    monthlyLineChartInstance = new Chart(
        document.getElementById('monthlyLineChart'),
        lineConfig
    );

    // --- Live Chart Functions ---

    function finishLiveChart() {
        if (chartInterval !== null) {
            clearInterval(chartInterval);
            chartInterval = null;
        }
        $('#StartLineChartButton').prop('disabled', true).text('Simulation Complete');
        $('#StopLineChartButton').prop('disabled', true).text('Finished');
    }

    function displayHistoricalData() {
        if (dataIndex >= historicalLineData.length) {
            finishLiveChart();
            return;
        }

        monthlyLineChartInstance.data.labels.push(historicalLineLabels[dataIndex]);
        monthlyLineChartInstance.data.datasets[0].data.push(historicalLineData[dataIndex]);

        monthlyLineChartInstance.update();
        dataIndex++;
    }

    function stopLiveChart() {
        if (chartInterval !== null) {
            clearInterval(chartInterval);
            chartInterval = null;
        } else {
            return;
        }

        $('#StartLineChartButton').prop('disabled', false).text('Resume Live Chart');
        $('#StopLineChartButton').prop('disabled', true).text('Paused');
    }

    function startLiveChart() {
        if (dataIndex >= historicalLineData.length) {
            return;
        }
        if (chartInterval !== null) {
            return;
        }

        const startButtonText = (dataIndex === 0) ? 'Running...' : 'Running... (Resumed)';

        chartInterval = setInterval(displayHistoricalData, 400);

        $('#StartLineChartButton').prop('disabled', true).text(startButtonText);
        $('#StopLineChartButton').prop('disabled', false).text('Stop Live Chart');
    }

    // --- Button Event Handlers (using jQuery) ---
    $('#StartLineChartButton').on('click', startLiveChart);
    $('#StopLineChartButton').on('click', stopLiveChart);

    // Set initial state
    $('#StartLineChartButton').prop('disabled', false).text('Start Live Chart');
    $('#StopLineChartButton').prop('disabled', true).text('Stop Live Chart');


    // =========================================================
    // 2. DOUGHNUT CHART SETUP (Permanent External Labels Added)
    // =========================================================

    if (doughnutLabels && doughnutLabels.length > 0) {
        const doughnutConfig = {
            type: 'doughnut',
            data: {
                labels: doughnutLabels,
                datasets: [{
                    label: 'Total Revenue (USD)',
                    data: doughnutData,
                    backgroundColor: [
                        '#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40'
                    ],
                    hoverOffset: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom' },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                let label = context.label || '';
                                if (label) { label += ': '; }
                                if (context.parsed !== null) {
                                    label += new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(context.parsed);
                                }
                                return label;
                            }
                        }
                    },
                    // DATALABELS CONFIGURATION FOR PERMANENT EXTERNAL LABELS
                    datalabels: {
                        color: '#333',         // Dark color for visibility outside the chart
                        anchor: 'end',          // Position the label at the outer edge of the slice
                        align: 'start',         // Align text away from the center
                        offset: 10,             // Distance from the chart edge
                        
                        // Leader lines are automatically drawn when offset is > 0 and position is outside
                        textAlign: 'left',      // Ensure the two lines are left-aligned
                        
                        formatter: function (value, context) {
                            // First line: Store Name
                            const label = context.chart.data.labels[context.dataIndex];
                            // Second line: Formatted Sales Amount
                            const formattedValue = new Intl.NumberFormat('en-US', { 
                                style: 'currency', 
                                currency: 'USD', 
                                minimumFractionDigits: 0, 
                                maximumFractionDigits: 0 
                            }).format(value);
                            
                            // Return an array to display the text on two lines
                            return [label, formattedValue];
                        },
                        font: { 
                            weight: 'bold', 
                            size: 12 
                        }
                    }
                }
            }
        };
        new Chart(
            document.getElementById('categoryDoughnutChart'),
            doughnutConfig
        );
    }
});