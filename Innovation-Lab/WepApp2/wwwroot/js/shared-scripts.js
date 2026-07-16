//---------- خاص بـ لوحة البيانات -----------

function showTab(id, event) {
    document.querySelectorAll('.chart-section').forEach(section => {
        section.classList.add('d-none');
    });
    document.getElementById(id).classList.remove('d-none');

    document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
    if (event && event.target) {
        event.target.classList.add('active');
    }
}
window.showTab = showTab;

// تم حذف: usageData / usageLabels / preferenceData / timeData وغيرها

// تم حذف: availabilityData + cardsContainer

// تصدير PDF
const pdfBtn = document.getElementById('exportPdfBtn');
if (pdfBtn) {
    pdfBtn.addEventListener('click', () => {
        const { jsPDF } = window.jspdf;
        const pdf = new jsPDF({ unit: 'pt', format: 'a4', direction: 'rtl' });
        pdf.text('تقرير لوحة البيانات', 40, 50);
        pdf.save('dashboard-report.pdf');
    });
}

// تصدير Excel
const excelBtn = document.getElementById('exportExcelBtn');
if (excelBtn) {
    excelBtn.addEventListener('click', () => {
        const wb = XLSX.utils.book_new();

        // ✅ هنا تقدر تجهز البيانات من خادم ASP.NET MVC عبر ViewBag أو AJAX
        const ws = XLSX.utils.aoa_to_sheet([
            ['العنصر', 'النسبة']
            // بيانات حقيقية تضاف هنا عند التحميل من السيرفر
        ]);

        XLSX.utils.book_append_sheet(wb, ws, 'تقرير');
        XLSX.writeFile(wb, 'dashboard-report.xlsx');
    });
}
