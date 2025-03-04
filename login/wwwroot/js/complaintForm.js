document.addEventListener("DOMContentLoaded", function () {
    // Fetch product names dynamically based on PO number
    const noPoInput = document.getElementById('no_po');
    if (noPoInput) {
        noPoInput.addEventListener('input', function () {
            const noPo = this.value.trim();

            if (noPo === '') {
                clearProductDropdown();
                return;
            }

            const token = document.querySelector('input[name="__RequestVerificationToken"]');

            fetch(`?handler=GetProductName&noPo=${encodeURIComponent(noPo)}`, {
                method: 'GET',
                headers: {
                    'RequestVerificationToken': token ? token.value : '',
                    'Accept': 'application/json'
                }
            })
                .then(response => {
                    if (!response.ok) {
                        throw new Error('Network response was not ok');
                    }
                    return response.json();
                })
                .then(data => {
                    const productNameSelect = document.getElementById('product_name');
                    productNameSelect.innerHTML = '';

                    if (data.success) {
                        if (data.products && data.products.length > 0) {
                            data.products.forEach(product => {
                                const option = document.createElement('option');
                                option.value = product.productName;
                                option.textContent = product.productName;
                                productNameSelect.appendChild(option);
                            });
                        } else {
                            const option = document.createElement('option');
                            option.value = '';
                            option.textContent = 'Produk tidak ditemukan';
                            productNameSelect.appendChild(option);
                        }
                    } else {
                        const option = document.createElement('option');
                        option.value = '';
                        option.textContent = data.message || 'Terjadi kesalahan saat memuat data';
                        productNameSelect.appendChild(option);
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    clearProductDropdown();
                    const productNameSelect = document.getElementById('product_name');
                    const option = document.createElement('option');
                    option.value = '';
                    option.textContent = 'Terjadi kesalahan saat memuat data';
                    productNameSelect.appendChild(option);
                });

            function clearProductDropdown() {
                const productNameSelect = document.getElementById('product_name');
                productNameSelect.innerHTML = '';
                const defaultOption = document.createElement('option');
                defaultOption.value = '';
                defaultOption.textContent = 'Pilih Nama Produk';
                productNameSelect.appendChild(defaultOption);
            }
        });
    }

    // Display selected file name and validate file size/type
    const fileInput = document.getElementById('file');
    const fileNameDisplay = document.getElementById('file-name');
    const maxFileSize = 5 * 1024 * 1024; // 5MB

    if (fileInput) {
        fileInput.addEventListener('change', function () {
            if (this.files && this.files.length > 0) {
                const file = this.files[0];
                const allowedTypes = ['application/pdf', 'image/jpeg', 'image/png'];

                if (!allowedTypes.includes(file.type)) {
                    alert('Format file tidak didukung. Harap unggah file PDF, JPG, atau PNG.');
                    this.value = ''; // Clear the input
                    fileNameDisplay.textContent = '';
                    return;
                }

                if (file.size > maxFileSize) {
                    alert('Ukuran file terlalu besar. Maksimum 5MB.');
                    this.value = ''; // Clear the input
                    fileNameDisplay.textContent = '';
                    return;
                }

                fileNameDisplay.textContent = file.name;
            } else {
                fileNameDisplay.textContent = '';
            }
        });
    }

    // Form validation
    const complaintForm = document.getElementById('complaintForm');
    if (complaintForm) {
        complaintForm.addEventListener('submit', function (event) {
            const emailInput = document.querySelector('input[type="email"]');
            const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

            if (!emailPattern.test(emailInput.value)) {
                event.preventDefault();
                alert('Mohon masukkan alamat email yang valid');
                emailInput.focus();
                return;
            }

            const productNameInput = document.getElementById('product_name');
            if (productNameInput.value.trim() === '') {
                event.preventDefault();
                alert('Pilih Nama Produk');
                productNameInput.focus();
            }
        });
    }
});