using System.Collections.Generic;

[System.Serializable]
public class QuestionList
{
    public List<QuestionData> questions; 
}

[System.Serializable]
public class QuestionData
{
    public string question;
    public string[] options;
    public string correctAnswer;
    public string[] image;
}