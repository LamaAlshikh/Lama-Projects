let sidebarCollapsed = false; // ✅ حالة تصغير القائمة الجانبية | Sidebar collapsed state

// =========================================================
// ✅ عند تحميل الصفحة
// =========================================================

// ربط تغيير الكلية (عند الإضافة) بدالة تحديث الأقسام
// Bind department updater to college dropdown
document.addEventListener("DOMContentLoaded", function () {
    const collegeSelect = document.getElementById("collegeSelectAdd");
    if (collegeSelect) {
        collegeSelect.addEventListener("change", updateDepartments);
    }
});

// استماع إضافي عند تحميل الصفحة - يمكن استخدامه لتوسعة لاحقة
// Placeholder listener on DOM load (for future features like Add/Edit User)
document.addEventListener("DOMContentLoaded", function () {
    // إضافة مستخدم (مخصص للتوسعة لاحقاً)
    // Edit user (placeholder)

});

// =========================================================
// ✅ دوال مساعدة لتنسيق الشارات (badges)
// =========================================================

// تحديد كلاس الشارة حسب حالة النشاط
// Get badge class based on user status
function getStatusBadgeClass(status) {
    switch (status) {
        case 'نشط': return 'status-active';
        case 'غير نشط': return 'status-inactive';
        default: return 'status-new';
    }
}

// تحديد كلاس الشارة حسب نوع المستخدم
// Get badge class based on user role
function getUserTypeBadgeClass(userType) {
    switch (userType) {
        case 'مشرف': return 'user-type-supervisor';
        case 'طالب': return 'user-type-student';
        case 'عضو هيئة تدريس': return 'user-type-faculty';
        case 'مدير بديل': return 'user-type-admin';
        default: return 'user-type-supervisor';
    }
}

// =========================================================
// ✅ دالة تعبئة جدول المستخدمين
// =========================================================

// Populate the users table dynamically
function populateUsersTable() {
    const tbody = document.getElementById('usersTableBody');
    tbody.innerHTML = ''; // تفريغ الجدول قبل إعادة التعبئة

    const filteredUsers = getFilteredUsers(); // تطبيق الفلاتر


    //console.log("🧪 أول عنصر:", filteredUsers[0]);

    filteredUsers.forEach(user => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>
                <div style="display: flex; align-items: center; gap: 12px;">
                    <div style="width: 28px; height: 28px; background: #007F3D; border-radius: 50%; display: flex; align-items: center; justify-content: center; color: white;">👥</div>
                    <span style="font-weight: 500;">${user.FirstName} ${user.LastName}</span>
                </div>
            </td>
            <td>${user.UserName}</td>
            <td><span class="status-badge ${getUserTypeBadgeClass(user.UserRole)}">${user.UserRole}</span></td>
            <td>${user.Email}</td>
            <td>${user.Faculty || '-'}</td>
            <td>${user.PhoneNumber || '-'}</td>
            <td>${formatLastLogin(user.LastLogIn)}</td>
            <td><span class="status-badge ${getStatusBadgeClass(user.IsActive ? 'نشط' : 'غير نشط')}">${user.IsActive ? 'نشط' : 'غير نشط'}</span></td>
            <td>
    <div class="action-buttons">
        <a class="action-btn btn-edit" href="/Users/EditUser/${user.UserID}" title="تعديل البيانات">
           ✏️  <span class="text-xs hidden sm:inline">تعديل</span>
        </a>
        
    </div>
</td>

        `;
        tbody.appendChild(row); // إضافة الصف إلى الجدول
    });
}

// =========================================================
// ✅ تطبيق الفلاتر الحالية على المستخدمين
// =========================================================

// Get users that match filter criteria
function getFilteredUsers() {
    const nameFilter = document.getElementById('nameFilter')?.value.toLowerCase() || '';
    const statusFilter = document.getElementById('statusFilter')?.value || '';
    const userTypeFilter = document.getElementById('userTypeFilter')?.value || '';

    return users.filter(user => {
        return (
            (`${user.FirstName} ${user.LastName}`.toLowerCase().includes(nameFilter)) &&
            (statusFilter === '' || (user.IsActive ? 'نشط' : 'غير نشط') === statusFilter) &&
            (userTypeFilter === '' || user.UserRole === userTypeFilter)
        );
    });
}

// تحديث الجدول بعد تغيير الفلاتر
// Refresh table on filter change
function filterUsers() {
    populateUsersTable();
}

// ✅ دالة لتنسيق تاريخ آخر تسجيل دخول
function formatLastLogin(dateStr) {
    if (!dateStr) return '—';

    const date = new Date(dateStr);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hour = String(date.getHours()).padStart(2, '0');
    const minute = String(date.getMinutes()).padStart(2, '0');

    return `${year}-${month}-${day} ${hour}:${minute}`;
}


// =========================================================
// ✅ التنقل إلى صفحة تعديل المستخدم
// =========================================================

// Navigate to edit page
function editUser(userId) {
    window.location.href = `/Users/EditUser/${userId}`;
}

// =========================================================
// ✅ التعامل مع الشاشة الصغيرة (الجوال)
// =========================================================

// Collapse sidebar on small screens
function checkScreenSize() {
    if (window.innerWidth <= 768 && !sidebarCollapsed) {
        toggleSidebar(); // يجب أن تكون هذه الدالة معرفة مسبقًا
    }
}

// =========================================================
// ✅ تهيئة الصفحة عند التحميل
// =========================================================

// On full page load
window.addEventListener('load', function () {
    populateUsersTable(); // تعبئة الجدول
    checkScreenSize(); // التحقق من الشاشة
});

// إعادة التحقق عند تغيير حجم الشاشة
// Re-check screen size on resize
window.addEventListener('resize', checkScreenSize);
