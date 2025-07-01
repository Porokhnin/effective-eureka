using Confluent.Kafka;

namespace cinema.events.Models.Requests
{
    /// <summary>
    /// Событие создания фильма
    /// </summary>
    internal class MovieCreated
    {
        /// <summary>
        /// Идентификатор фильма
        /// </summary>
        public int MovieId { get; init; }

        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        public int UserId { get; init; }

        /// <summary>
        /// Заголовок
        /// </summary>
        public required string Title { get; init; }

        /// <summary>
        /// Тип действия
        /// </summary>
        public required string Action { get; init; }


        public override string ToString()
        {
            return $"MovieId:{MovieId}, UserId:{UserId}, Title:{Title}, Action:{Action}";
        }
    }
}