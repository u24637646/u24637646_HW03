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

    // FIX 1: Defined lineConfig BEFORE it's used to initialize the chart
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
    // CRITICAL: Expose the chart instance globally for the save form logic
    window.monthlyLineChart = monthlyLineChartInstance;


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

        // Fix from previous round: ensure update is called and index increments
        monthlyLineChartInstance.update();
        dataIndex++;
    }

    // --- Live Chart Event Listeners ---
    $('#StartLineChartButton').on('click', function () {
        if (chartInterval === null) {
            // Reset logic for restarting simulation
            if (dataIndex >= historicalLineData.length) {
                monthlyLineChartInstance.data.labels = [];
                monthlyLineChartInstance.data.datasets[0].data = [];
                dataIndex = 0;
                monthlyLineChartInstance.update();
                $('#StartLineChartButton').prop('disabled', false).text('Start Live Chart');
                $('#StopLineChartButton').prop('disabled', false).text('Stop Live Chart');
            }

            chartInterval = setInterval(displayHistoricalData, 1000);
            $('#StartLineChartButton').text('Running...');
        }
    });

    $('#StopLineChartButton').on('click', function () {
        if (chartInterval !== null) {
            clearInterval(chartInterval);
            chartInterval = null;
            $('#StartLineChartButton').text('Resume Live Chart');
            $('#StopLineChartButton').text('Stopped');
        }
    });


    // =========================================================
    // 2. DOUGHNUT CHART SETUP 
    // =========================================================

    function generateRandomColors(count) {
        const colors = [];
        for (let i = 0; i < count; i++) {
            const r = Math.floor(Math.random() * 255);
            const g = Math.floor(Math.random() * 255);
            const b = Math.floor(Math.random() * 255);
            colors.push(`rgb(${r}, ${g}, ${b})`);
        }
        return colors;
    }

    // FIX 2: Defined doughnutConfig BEFORE it's used to initialize the chart
    const doughnutConfig = {
        type: 'doughnut',
        data: {
            labels: doughnutLabels,
            datasets: [{
                label: 'Total Sales Revenue (ZAR)',
                data: doughnutData,
                backgroundColor: generateRandomColors(doughnutLabels.length),
                hoverOffset: 4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                },
                title: {
                    display: false,
                    text: 'Sales Revenue Distribution by Store'
                }
            }
        }
    };

    // Initialize the Doughnut Chart.js instance
    window.categoryDoughnutChart = new Chart(
        document.getElementById('categoryDoughnutChart'),
        doughnutConfig
    );

    // =========================================================
    // 3. PDF GENERATION UTILITY FUNCTION
    // =========================================================

    window.getChartDocDef = function (chartTitle, chartObject, descriptionHtml) {
        const chartBase64Image = chartObject.toBase64Image();

        function htmlToPdfMake(html) {
            const doc = [];
            const parser = new DOMParser();
            const docEl = parser.parseFromString(html, 'text/html').body;

            // Handle simple paragraphs
            docEl.childNodes.forEach(node => {
                if (node.nodeType === 1 && node.tagName === 'P') {
                    doc.push({ text: node.textContent, margin: [0, 5, 0, 0] });
                }
            });

            if (doc.length === 0 && descriptionHtml.trim().length > 0) {
                doc.push({ text: descriptionHtml, margin: [0, 5, 0, 0] });
            }
            return doc;
        }

        const descriptionContent = htmlToPdfMake(descriptionHtml);

        return {
            content: [
                { text: chartTitle, style: 'header' },
                { text: `Report Generated: ${new Date().toLocaleDateString()}`, margin: [0, 5, 0, 20] },

                // Chart Image
                {
                    image: chartBase64Image,
                    width: 500,
                    alignment: 'center',
                    margin: [0, 20, 0, 20]
                },

                // Description Section
                { text: 'Description:', style: 'subheader', margin: [0, 10, 0, 5] },
                ...descriptionContent
            ],
            styles: {
                header: {
                    fontSize: 18,
                    bold: true
                },
                subheader: {
                    fontSize: 15,
                    bold: true
                },
                quote: {
                    italics: true
                }
            }
        };
    };
});