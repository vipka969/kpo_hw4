# Gоzон - Интернет-магазин

## Обзор системы

**Gоzон** - микросервисная система интернет-магазина, разработанная для обработки высоких нагрузок в преддверии новогодних распродаж. Система обеспечивает надежное асинхронное взаимодействие между сервисами заказов и платежей с гарантиями доставки сообщений и идемпотентной обработкой операций.

### Основные цели проекта:
1. Обеспечение высокой доступности при пиковых нагрузках
2. Гарантированная доставка сообщений между сервисами
3. Exactly-once семантика при списании денежных средств
4. Масштабируемость каждого компонента независимо
5. Отказоустойчивость через асинхронную коммуникацию

## Архитектура

### Компоненты системы:

| Сервис | Назначение | Технологии | Порт |
|--------|------------|------------|------|
| **Order Service** | Управление жизненным циклом заказов | ASP.NET Core 8, Entity Framework, PostgreSQL | 5001 |
| **Payments Service** | Управление счетами и обработка платежей | ASP.NET Core 8, Entity Framework, PostgreSQL | 5002 |
| **API Gateway** | Единая точка входа, маршрутизация запросов | ASP.NET Core 8 | 8080 |
| **Frontend** | Веб-интерфейс для пользователей | HTML5, CSS3, JavaScript, Bootstrap 5 | 3000 |
| **Redpanda** | Брокер сообщений для асинхронной связи | Redpanda (Kafka-совместимый) | 29092 |
| **PostgreSQL** | Хранение данных заказов и платежей | PostgreSQL 15 | 5432/5433 |

### Поток данных:
```
Пользователь → Frontend → API Gateway → Order Service → Kafka → Payments Service → Kafka → Order Service
```

### Реализованные паттерны:
1. Transactional Outbox в Order Service (для отправки OrderCreatedEvent)
2. Transactional Inbox + Outbox в Payments Service (для обработки OrderCreatedEvent и отправки PaymentProcessedEvent)
3. Idempotent Consumer в обоих сервисах (обработка дублирующихся сообщений)
4. API Gateway Pattern для единой точки входа
5. Event-Driven Architecture через Kafka

## Запуск системы

### Предварительные требования:
- Docker 20.10+
- Docker Compose 2.0+
- 4 ГБ свободной оперативной памяти
- 2 ГБ свободного дискового пространства

### Полная инструкция по запуску:

#### Шаг 1: Запуск всех сервисов
```bash
# Запуск с пересборкой образов (первый запуск)
docker-compose up --build

# Или запуск в фоновом режиме
docker-compose up -d --build
```

#### Шаг 2: Проверка доступности
```bash
# Проверка здоровья всех сервисов
curl http://localhost:8080/health      # API Gateway
curl http://localhost:5001/health      # Order Service
curl http://localhost:5002/health      # Payments Service
```

## API Endpoints

### Order Service (через API Gateway)
| Метод | Endpoint | Описание | Заголовки | Тело запроса |
|-------|----------|----------|-----------|--------------|
| POST | `/orders` | Создание нового заказа | `X-User-Id: GUID` | `{"amount": decimal, "description": "string"}` |
| GET | `/orders` | Получение списка заказов пользователя | `X-User-Id: GUID` | - |
| GET | `/orders/{id}` | Получение заказа по ID | `X-User-Id: GUID` | - |

### Payments Service (через API Gateway)
| Метод | Endpoint | Описание | Заголовки | Тело запроса |
|-------|----------|----------|-----------|--------------|
| POST | `/payments/accounts` | Создание счета пользователя | `X-User-Id: GUID` | `{"Balance": decimal}` |
| POST | `/payments/accounts/deposit` | Пополнение счета | `X-User-Id: GUID` | `{"Amount": decimal}` |
| GET | `/payments/accounts` | Получение информации о счете | `X-User-Id: GUID` | - |

## Основное задание

### Реализованные требования:

#### Функциональность
- Создание счета (не более одного на пользователя)
- Пополнение счета
- Просмотр баланса счета
- Создание заказа с асинхронным запуском оплаты
- Просмотр списка заказов
- Просмотр статуса отдельного заказа

#### Архитектурное проектирование
**a. Четкое разделение на сервисы:**
- Order Service
- Payments Service
- Полная независимость сервисов, разные БД

**b. Логичное использование Kafka:**
- Топик `orders.created`: события создания заказов
- Топик `payments.processed`: результаты обработки платежей
- At-least-once доставка с ручным коммитом offset
- Exactly-once семантика через идемпотентную обработку

**c. Применение паттернов:**
- **Transactional Outbox в Order Service**: `OutboxProcessor` отправляет OrderCreatedEvent
- **Transactional Inbox и Outbox в Payments Service**:
    - `KafkaConsumer` → `InboxProcessor` → `OutboxProcessor`
- **Exactly-once при списании денег**: Idempotency через проверку MessageId в Inbox

**d. Транспорт между микросервисами:**
- Redpanda (Kafka-совместимый) с at-least-once гарантиями
- Каждый сервис сохраняет offset и обеспечивает идемпотентность
- Автоматическое создание топиков при запуске

#### Swagger

- OrderService: `http://localhost:5001/swagger`
- PaymentsService: `http://localhost:5002/swagger`
- ApiGateway: `http://localhost:8080/swagger`
  
#### Docker конфигурация 
- Все микросервисы в Docker-контейнерах
- Полная система через docker-compose.yml
- Работоспособность после `docker compose up`
-  Health checks для всех сервисов
- Зависимости между контейнерами

## Дополнительное задание: Веб-фронтенд 

### Полное описание реализации

#### Цели фронтенда:
1. Предоставить интуитивно понятный интерфейс для тестирования системы
2. Визуализировать асинхронный поток событий между сервисами
3. Демонстрировать работу паттернов Inbox/Outbox в реальном времени

#### Техническая реализация:

**Стек технологий:**
- **HTML5** - семантическая разметка
- **CSS3** с Bootstrap 5 - адаптивный дизайн
- **Vanilla JavaScript** (ES6+) - логика приложения
- **nginx** - веб-сервер для статики
- **Docker** - контейнеризация

**Архитектурные решения:**
- Модульная структура компонентов
- Event-driven UI с реактивными обновлениями
- Локальное хранение User ID в sessionStorage
- Интегрированная панель логов для отладки

#### Структура проекта Frontend:
```
Frontend/
├── Dockerfile                    
├── nginx.conf               
├── index.html              
├── app.js                     
├── style.css                   
└── README.md  
```

#### Особенности интерфейса:

**1. Четырехуровневая навигация:**
```
[Выбор User ID] → [Управление счетом] → [Создание заказа] → [История заказов]
```

**2. Интерактивные элементы:**
- Генератор UUID в реальном времени
- Быстрые действия для пополнения счета (+100, +500, +1000)
- Валидация форм на стороне клиента
- Индикаторы загрузки для асинхронных операций
- Цветовые индикаторы статусов (New, Finished, Cancelled)

**3. Панель мониторинга:**
- Живые логи операций с таймстемпами
- Статус подключения к бэкенду
- Счетчики операций (созданные заказы, пополнения)
- Визуализация баланс* через прогресс-бар

#### Пошаговая инструкция по использованию:

**Шаг 0: Подготовка системы**
```bash
# Убедитесь что все сервисы запущены
docker-compose ps

# Должны быть активны 7 контейнеров:
# 1. redpanda
# 2. postgres-order
# 3. postgres-payments
# 4. order-service
# 5. payments-service
# 6. api-gateway
# 7. frontend
```

**Шаг 1: Открытие фронтенда**
```
Откройте браузер и перейдите по адресу:
http://localhost:3000
```

**Шаг 2: Инициализация пользователя**
```
Нужно сначал создать ID пользователя через терминал, а затем 
скопировать его и вставить на сайт.
```

**Шаг 3: Работа с системой**

```
1. В разделе "Мой счет" нажмите "+500 ₽" (баланс: 1500 ₽)
2. В разделе "Создание заказа":
   - Введите сумму: 250.50
   - Введите описание: "Новогодний подарок"
   - Нажмите "Создать заказ"
3. В панели логов увидите:
   [14:25:33] Заказ создан! ID: 9c5f6192-5973-4612-b134-c863377371e3
   [14:25:43] Payment processed successfully for order 9c5f6192...
4. Нажмите "Обновить" в разделе счета (баланс: 1249.50 ₽)
5. Нажмите "Обновить список" в заказах
   - Статус заказа изменится с "New" → "Finished"
```

## Технические детали

### Конфигурация Kafka (Redpanda):
```yaml
redpanda:
  image: docker.redpanda.com/redpandadata/redpanda:v23.2.13
  command:
    - redpanda
    - start
    - --smp 1
    - --memory 1G
    - --kafka-addr PLAINTEXT://0.0.0.0:29092
    - --advertise-kafka-addr PLAINTEXT://redpanda:29092
```

### Конфигурация базы данных:
```yaml
postgres-order:
  environment:
    POSTGRES_DB: orders
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: postgres

postgres-payments:
  environment:
    POSTGRES_DB: payments
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: postgres
```

### Конфигурация сервисов:
```yaml
order-service:
  environment:
    Kafka__BootstrapServers: redpanda:29092
    Kafka__OrdersTopic: orders.created
    Kafka__PaymentsTopic: payments.processed

payments-service:
  environment:
    Kafka__BootstrapServers: redpanda:29092
    Kafka__OrdersTopic: orders.created
    Kafka__PaymentsTopic: payments.processed
```

