using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Mappers
{
    public class FailureMessage
    {
        public static string GetString(Failure failure)
        {
            switch (failure)
            {
                case Failure.NotPossibleEditDeletedArticle:
                    return "It is not possible to edit a deleted article";
                case Failure.UndefinedLanguage:
                    return "It is not possible to edit an article for an undefined language";
                case Failure.NotMarkedAsDeleted:
                    return "Not possible to delete an article that is not marked as deleted";
                case Failure.ArticleNotFound:
                    return "Article not found";
                case Failure.BackupNotFound:
                    return "No article backup was found that matches that date and language";
                case Failure.InvalidInput:
                    return "Invalid input. Make sure that it is correctly formed and that it is not null";
                case Failure.DeploymentLaunchFailed:
                    return "It was not possible to launch the deployment. Please try later or check Github for more information.";
                case Failure.EmailWasNotSent:
                    return "The email was not send. The email provider service was not able to send the email.";
                case Failure.EmailMessageIsEmpty:
                    return "Email message is empty. Please provide a message";
                case Failure.AdministratorRequired:
                    return "There should be at least one Administrator left";
                case Failure.UserNotFound:
                    return "User with the provided ID was not found";
                case Failure.UserAlreadyExists:
                    return "User with same email address already exists";
                case Failure.LanguageRequired:
                    return "At least one language is required";
                case Failure.FolderNotInitialized:
                    return "Folder was not initialized";
                case Failure.InvalidStartDate:
                    return "Invalid start date";
                case Failure.InvalidEndDate:
                    return "Invalid end date";
                default:
                    return "";
            }
        }
    }
}
