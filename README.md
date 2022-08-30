# GeekBrains Message Driven Architecture
## Описание
Реализация домашних заданий по реализации MDA на примере бронирования в ресторане, где сервисы бронирования, кухни и уведомления общаются между собой через MassTransit RabbitMQ,  финальный вариант
## В проекте
* Repositories - проект с репозиториями для реализации идемпотентности сообщений. 2 типа - in memory и в базе данных
* Restaurant.Booking - проект с реализацией заказа/отмены столиков. Там же Saga с настройкой событий в MassTransit
* Restaurant.Kitchen - проект обработки заказа на кухне
* Restaurant.Notification - проект с реализацией отсылки уведомлений.
* Restaurant.Messages - абстракции
* Restaurant.Tests - тесты для MassTransit консьюмеров и саги