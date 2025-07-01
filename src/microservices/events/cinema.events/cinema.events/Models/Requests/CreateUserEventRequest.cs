namespace cinema.events.Models.Requests
{
    /// <summary>
    /// Запрос на создание события пользователя
    /// </summary>
    internal class CreateUserEventRequest
    {
        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        public int UserId { get; init; }

        /// <summary>
        /// Имя пользователя
        /// </summary>
        public required string Username { get; init; }

        /// <summary>
        /// Тип действия пользователя
        /// </summary>
        public required string Action { get; init; }

        /// <summary>
        /// Время
        /// </summary>
        public required DateTimeOffset Timestamp { get; init; }


        public override string ToString()
        {
            return $"UserId:{UserId}, Username:{Username}, Action:{Action}, Timestamp:{Timestamp}";
        }
    }
}