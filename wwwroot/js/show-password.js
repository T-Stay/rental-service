// Hiển thị/ẩn mật khẩu cho các trường password
function togglePasswordVisibility(inputId, btnId) {
    var input = document.getElementById(inputId);
    var btn = document.getElementById(btnId);
    if (input.type === "password") {
        input.type = "text";
        btn.innerHTML = '<i class="fa fa-eye-slash"></i>';
    } else {
        input.type = "password";
        btn.innerHTML = '<i class="fa fa-eye"></i>';
    }
}
