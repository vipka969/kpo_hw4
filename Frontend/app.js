const API_BASE_URL = 'http://localhost:8080';

let currentUserId = null;

function log(message, type = 'info') {
    const logOutput = document.getElementById('logOutput');
    const time = new Date().toLocaleTimeString();
    const logEntry = document.createElement('div');
    logEntry.className = `log-entry log-${type}`;
    logEntry.innerHTML = `<span class="log-time">[${time}]</span> ${message}`;
    logOutput.prepend(logEntry);

    console.log(`[${type.toUpperCase()}] ${message}`);
}

function showError(message) {
    log(`Ошибка: ${message}`, 'error');
    alert(`Ошибка: ${message}`);
}

function showSuccess(message) {
    log(`${message}`, 'success');
}

function clearLogs() {
    document.getElementById('logOutput').innerHTML = '';
    log('Логи очищены');
}

function generateUserId() {
    const uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
    document.getElementById('userIdInput').value = uuid;
    log(`Сгенерирован новый User ID: ${uuid}`);
}

function setUserId() {
    const userIdInput = document.getElementById('userIdInput').value.trim();

    const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

    if (!guidRegex.test(userIdInput)) {
        showError('Неверный формат User ID. Используйте формат GUID.');
        return;
    }

    currentUserId = userIdInput;
    document.getElementById('currentUserId').textContent = userIdInput;

    document.getElementById('accountSection').style.display = 'block';
    document.getElementById('orderSection').style.display = 'block';
    document.getElementById('ordersListSection').style.display = 'block';

    showSuccess(`User ID установлен: ${userIdInput}`);
    loadAccount();
    loadOrders();
}

async function makeRequest(endpoint, method = 'GET', data = null) {
    const url = `${API_BASE_URL}${endpoint}`;
    const options = {
        method: method,
        headers: {
            'Content-Type': 'application/json',
            'X-User-Id': currentUserId
        }
    };

    if (data && (method === 'POST' || method === 'PUT')) {
        options.body = JSON.stringify(data);
    }

    try {
        log(`Отправка запроса: ${method} ${endpoint}`);
        const response = await fetch(url, options);

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        return await response.json();
    } catch (error) {
        throw error;
    }
}

async function loadAccount() {
    if (!currentUserId) {
        showError('Сначала выберите User ID');
        return;
    }

    try {
        log('Загрузка информации о счете...');
        const account = await makeRequest('/payments/accounts');

        if (account) {
            displayAccountInfo(account);
            showSuccess('Информация о счете загружена');
        } else {
            document.getElementById('accountInfo').innerHTML = `
                <div class="alert alert-warning">
                    <i class="bi bi-exclamation-triangle"></i> Счет не найден
                </div>
            `;
        }
    } catch (error) {
        showError(`Не удалось загрузить счет: ${error.message}`);
    }
}

function displayAccountInfo(account) {
    const accountInfo = document.getElementById('accountInfo');
    const balanceColor = account.balance > 0 ? 'text-success' : 'text-danger';

    accountInfo.innerHTML = `
        <div class="row">
            <div class="col-md-6">
                <h5>Информация о счете:</h5>
                <table class="table table-bordered">
                    <tr>
                        <th>Баланс:</th>
                        <td class="fw-bold ${balanceColor}">${account.balance.toFixed(2)} ₽</td>
                    </tr>
                    <tr>
                        <th>Статус:</th>
                        <td>
                            <span class="badge ${account.status === 'Active' ? 'bg-success' : 'bg-warning'}">
                                ${account.status}
                            </span>
                        </td>
                    </tr>
                    <tr>
                        <th>Создан:</th>
                        <td>${new Date(account.createdAt).toLocaleString()}</td>
                    </tr>
                </table>
            </div>
            <div class="col-md-6">
                <h5>Статистика:</h5>
                <div class="card">
                    <div class="card-body">
                        <p><i class="bi bi-cash-stack"></i> Доступно: <strong>${account.balance.toFixed(2)} ₽</strong></p>
                        <div class="progress mt-2">
                            <div class="progress-bar" role="progressbar" 
                                 style="width: ${Math.min(account.balance / 1000 * 100, 100)}%">
                                ${account.balance.toFixed(0)} ₽
                            </div>
                        </div>
                        <small class="text-muted">Прогресс до 1000 ₽</small>
                    </div>
                </div>
            </div>
        </div>
    `;
}

async function createAccount() {
    if (!currentUserId) {
        showError('Сначала выберите User ID');
        return;
    }

    try {
        log('Создание нового счета...');
        const account = await makeRequest('/payments/accounts', 'POST', {
            Balance: 1000.00
        });

        displayAccountInfo(account);
        showSuccess('Счет успешно создан с начальным балансом 1000 ₽');
    } catch (error) {
        showError(`Не удалось создать счет: ${error.message}`);
    }
}

async function depositMoney() {
    if (!currentUserId) {
        showError('Сначала выберите User ID');
        return;
    }

    const amount = prompt('Введите сумму для пополнения:', '500');
    if (!amount || isNaN(amount) || parseFloat(amount) <= 0) {
        showError('Неверная сумма');
        return;
    }

    try {
        log(`Пополнение счета на ${amount} ₽...`);
        const account = await makeRequest('/payments/accounts/deposit', 'POST', {
            Amount: parseFloat(amount)
        });

        displayAccountInfo(account);
        showSuccess(`Счет пополнен на ${amount} ₽`);
    } catch (error) {
        showError(`Не удалось пополнить счет: ${error.message}`);
    }
}

function quickDeposit(amount) {
    if (!currentUserId) {
        showError('Сначала выберите User ID');
        return;
    }

    if (confirm(`Пополнить счет на ${amount} ₽?`)) {
        makeRequest('/payments/accounts/deposit', 'POST', { Amount: amount })
            .then(account => {
                displayAccountInfo(account);
                showSuccess(`Счет пополнен на ${amount} ₽`);
            })
            .catch(error => {
                showError(`Не удалось пополнить счет: ${error.message}`);
            });
    }
}

async function createOrder() {
    if (!currentUserId) {
        showError('Сначала выберите User ID');
        return;
    }

    const amount = document.getElementById('orderAmount').value;
    const description = document.getElementById('orderDescription').value;

    if (!amount || isNaN(amount) || parseFloat(amount) <= 0) {
        showError('Введите корректную сумму заказа');
        return;
    }

    if (!description.trim()) {
        showError('Введите описание заказа');
        return;
    }

    try {
        log(`Создание заказа: ${description} на сумму ${amount} ₽...`);
        const order = await makeRequest('/orders', 'POST', {
            Amount: parseFloat(amount),
            Description: description
        });

        document.getElementById('orderAmount').value = '';
        document.getElementById('orderDescription').value = '';

        showSuccess(`Заказ создан! ID: ${order.id}`);

        setTimeout(() => {
            loadAccount();
            loadOrders();
        }, 1000);
    } catch (error) {
        showError(`Не удалось создать заказ: ${error.message}`);
    }
}

async function loadOrders() {
    if (!currentUserId) {
        showError('Сначала выберите User ID');
        return;
    }

    try {
        log('Загрузка списка заказов...');
        const orders = await makeRequest('/orders');

        displayOrdersList(orders || []);
        showSuccess(`Загружено ${orders?.length || 0} заказов`);
    } catch (error) {
        showError(`Не удалось загрузить заказы: ${error.message}`);
    }
}

function displayOrdersList(orders) {
    const ordersList = document.getElementById('ordersList');

    if (!orders || orders.length === 0) {
        ordersList.innerHTML = `
            <div class="alert alert-info">
                <i class="bi bi-info-circle"></i> У вас пока нет заказов
            </div>
        `;
        return;
    }

    let html = `
        <div class="table-responsive">
            <table class="table table-hover">
                <thead class="table-light">
                    <tr>
                        <th>ID</th>
                        <th>Сумма</th>
                        <th>Описание</th>
                        <th>Статус</th>
                        <th>Дата</th>
                    </tr>
                </thead>
                <tbody>
    `;

    orders.forEach(order => {
        const statusBadge = getStatusBadge(order.status);
        const shortId = order.id.substring(0, 8) + '...';
        const date = new Date(order.createdDate).toLocaleDateString();

        html += `
            <tr>
                <td><small>${shortId}</small></td>
                <td class="fw-bold">${order.amount.toFixed(2)} ₽</td>
                <td>${order.description}</td>
                <td>${statusBadge}</td>
                <td>${date}</td>
            </tr>
        `;
    });

    html += `
                </tbody>
            </table>
        </div>
    `;

    ordersList.innerHTML = html;
}

function getStatusBadge(status) {
    const badges = {
        'New': 'badge bg-secondary',
        'Finished': 'badge bg-success',
        'Cancelled': 'badge bg-danger',
        'Processing': 'badge bg-warning'
    };

    const badgeClass = badges[status] || 'badge bg-info';
    return `<span class="${badgeClass}">${status}</span>`;
}

document.addEventListener('DOMContentLoaded', function() {
    log('Приложение Gоzон запущено');

    generateUserId();
});