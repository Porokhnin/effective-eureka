namespace cinema.events.Models.Requests
{
    /// <summary>
    /// Запрос на создание события оплаты
    /// </summary>
    internal class CreatePaymentEventRequest
    {
        /// <summary>
        /// Идентификатор платежа
        /// </summary>
        public int PaymentId { get; init; }

        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        public int UserId { get; init; }

        /// <summary>
        /// Сумма
        /// </summary>
        public required decimal Amount { get; init; }

        /// <summary>
        /// Статус
        /// </summary>
        public required string Status { get; init; }

        /// <summary>
        /// Время
        /// </summary>
        public required DateTimeOffset Timestamp { get; init; }

        /// <summary>
        /// Тип оплаты
        /// </summary>
        public required string MethodType { get; init; }


        public override string ToString()
        {
            return $"PaymentId:{PaymentId}, UserId:{UserId}, Amount:{Amount}, Status:{Status}, MethodType:{MethodType}, Timestamp:{Timestamp}";
        }
    }
}