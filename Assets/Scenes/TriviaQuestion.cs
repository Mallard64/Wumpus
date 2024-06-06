using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriviaQuestion
{
    public string Question { get; set; }
    public string Answer { get; set; }
    public string Answer1 { get; set; }
    public string Answer2 { get; set; }
    public string Answer3 { get; set; }
    public string Answer4 { get; set; }

    public TriviaQuestion(string question, string answer, string answer1, string answer2, string answer3, string answer4)
    {
        Question = question;
        Answer = answer;
        Answer1 = answer1;
        Answer2 = answer2;
        Answer3 = answer3;
        Answer4 = answer4;
    }
}

