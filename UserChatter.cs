using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskNotissimusParse
{
    public class UserChatter
    {
        public Action<string> NotifyUserHandler { get; }
        public Func<string> GetUserAnswerHandler { get; }

        public UserChatter(Action<string> notifyUserHandler, Func<string> getUserAnswerHandler)
        {
            NotifyUserHandler = notifyUserHandler;
            GetUserAnswerHandler = getUserAnswerHandler;
        }

        public void NotifyUser(string message) => NotifyUserHandler?.Invoke(message);
        
        public string GetAnswerFromUser(string question, Predicate<string>? checkAnswerPredicate = null)
        {
            while (true)
            {
                NotifyUserHandler?.Invoke(question);
                var answer = GetUserAnswerHandler();

                if (checkAnswerPredicate != null && checkAnswerPredicate(answer))
                {
                    return answer;
                }
                else
                {
                    NotifyUserHandler?.Invoke("Ошибка ввода. Повторите ввод.");
                }
            }
        }
    }
}
