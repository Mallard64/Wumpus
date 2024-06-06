using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriviaGenerator : MonoBehaviour
{
    private List<TriviaQuestion> questions;
    private System.Random rnd = new System.Random();

    public TriviaGenerator()
    {
        questions = new List<TriviaQuestion>
        {
            new TriviaQuestion("Who is the creator of Hunt the Wumpus?", "Gregory Yob", "Gregory Yob", "Albert Einstein", "Bobby Fletcher", "Erin White"),
            new TriviaQuestion("In what year was Hunt the Wumpus originally released?", "1973", "1934", "1978", "1967", "1973"),
            new TriviaQuestion("What programming language was the original Hunt the Wumpus written in?", "BASIC", "Python", "C++", "Assembly", "BASIC"),
        };
    }

    public TriviaQuestion GetRandomQuestion()
    {
        int index = rnd.Next(questions.Count);
        return questions[index];
    }

    
}

