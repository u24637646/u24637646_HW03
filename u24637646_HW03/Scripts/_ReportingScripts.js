document.addEventListener('DOMContentLoaded', function () {

    // Fetch all initial data structures passed from the server-side view.
    const historicalLineLabels = window.lineLabels;
    const historicalLineData = window.lineData;
    const doughnutLabels = window.DoughnutLabels;
    const doughnutData = window.DoughnutData;

    // Variables to control the 'live' chart simulation and track its progress.
    let chartInterval = null;
    let monthlyLineChartInstance = null;
    let dataIndex = 0;

    // --- Historical Order Chart Setup ---

    // Configuration object defining the appearance and behavior of the monthly line chart.
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
                legend: { display: false }
            }
        }
    };

    // Create and render the Chart.js line graph.
    monthlyLineChartInstance = new Chart(
        document.getElementById('monthlyLineChart'),
        lineConfig
    );

    // Make the chart instance globally available so the PDF export function can access it later.
    window.monthlyLineChart = monthlyLineChartInstance;


    // --- Functions for the Live Data Simulation ---

    // Stops the live data simulation and updates the controls.
    function finishLiveChart() {
        if (chartInterval !== null) {
            clearInterval(chartInterval);
            chartInterval = null;
        }
        $('#StartLineChartButton').prop('disabled', true).text('Simulation Complete');
        $('#StopLineChartButton').prop('disabled', true).text('Finished');
    }

    // Pushes the next month's data point and label, then redraws the chart.
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

    // --- User Controls for the Line Chart ---
    $('#StartLineChartButton').on('click', function () {
        if (chartInterval === null) {
            // Handles starting the simulation, including resetting the chart if it had finished.
            if (dataIndex >= historicalLineData.length) {
                monthlyLineChartInstance.data.labels = [];
                monthlyLineChartInstance.data.datasets[0].data = [];
                dataIndex = 0;
                monthlyLineChartInstance.update();
                $('#StartLineChartButton').prop('disabled', false).text('Start Live Chart');
                $('#StopLineChartButton').prop('disabled', false).text('Stop Live Chart');
            }

            // Start the interval timer to incrementally display the data points every half-second.
            chartInterval = setInterval(displayHistoricalData, 500);
            $('#StartLineChartButton').text('Running...');
        }
    });

    $('#StopLineChartButton').on('click', function () {
        if (chartInterval !== null) {
            // Clears the interval to pause the animation.
            clearInterval(chartInterval);
            chartInterval = null;
            $('#StartLineChartButton').text('Resume Live Chart');
            $('#StopLineChartButton').text('Stopped');
        }
    });


    // --- Sales Revenue Distribution Chart Setup ---

    // A fixed color scheme used for visual consistency across all slices.
    const fixedDoughnutColors = [
        'rgb(255, 99, 132)', // Red
        'rgb(54, 162, 235)', // Blue
        'rgb(255, 205, 86)', // Yellow
        'rgb(75, 192, 192)', // Green
        'rgb(153, 102, 255)', // Purple
        'rgb(255, 159, 64)', // Orange
        'rgb(201, 203, 207)' // Grey (if needed for more data points)
    ];

    // Configuration object for the doughnut chart.
    const doughnutConfig = {
        type: 'doughnut',
        data: {
            labels: doughnutLabels,
            datasets: [{
                label: 'Total Sales Revenue (ZAR)',
                data: doughnutData,

                // Apply the fixed color palette to the data slices.
                backgroundColor: fixedDoughnutColors.slice(0, doughnutLabels.length),
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

    // Create and render the Chart.js doughnut chart.
    window.categoryDoughnutChart = new Chart(
        document.getElementById('categoryDoughnutChart'),
        doughnutConfig
    );

    // --- PDF Export Logic ---

    // Generates the document definition for pdfmake using chart data and a description.
    window.getChartDocDef = function (chartTitle, chartObject, descriptionHtml) {

        // Get the chart's current state as a base64 image string for PDF inclusion.
        const chartBase64Image = chartObject.toBase64Image();

        // Utility function to convert simple HTML paragraph tags into pdfmake content objects.
        function htmlToPdfMake(html) {
            const doc = [];
            const parser = new DOMParser();
            const docEl = parser.parseFromString(html, 'text/html').body;

            // Iterate through HTML elements to extract text content.
            docEl.childNodes.forEach(node => {
                if (node.nodeType === 1 && node.tagName === 'P') {
                    doc.push({ text: node.textContent, margin: [0, 5, 0, 0] });
                }
            });

            // If no standard elements were found, use the raw HTML content as a fallback.
            if (doc.length === 0 && descriptionHtml.trim().length > 0) {
                doc.push({ text: descriptionHtml, margin: [0, 5, 0, 0] });
            }
            return doc;
        }

        const descriptionContent = htmlToPdfMake(descriptionHtml);

        return {
            // Adds default styling for the PDF document.
            defaultStyle: {
                fontSize: 10,
                font: 'Roboto'
            },
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
                // Insert the chart description content into the main PDF document array.
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