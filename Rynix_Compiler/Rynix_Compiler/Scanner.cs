using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rynix_Compiler
{
    public enum Token_Class
    {
        Int, Float, String, Bool, Char, Array, Void,

        LineComment, MultiLineComment,

        Identifier, number, Constant, StringLiteral, CharLiteral,

        Var, Val, Fun, Class, If, Else, While, For, In,
        Return, True, False, Null, New, Print, Input,
        Break, Continue, Init,

        PlusOp, MinusOp, DivideOp, MultiplyOp, ModulusOp,
        Assign, Less, Greater, Equal, Notequal,
        Lessorequal, Greaterorequal,

        Dot, Range, Comma, Colon,
        LParen, RParen, LBrace, RBrace,
        LBracket, RBracket
    }

    public class Token
    {
        public string lex;
        public Token_Class token_type;
    }

    public class Scanner
    {
        public List<Token> Tokens = new List<Token>();

        private readonly Dictionary<string, Token_Class> ReservedWords =
            new Dictionary<string, Token_Class>();

        private readonly Dictionary<string, Token_Class> Operators =
            new Dictionary<string, Token_Class>();

        private readonly Regex WhiteSpaceRegex =
            new Regex(@"\G\s", RegexOptions.Compiled);

        private readonly Regex LineCommentRegex =
            new Regex(@"\G//[^\r\n]*", RegexOptions.Compiled);

        private readonly Regex MultiLineCommentRegex =
            new Regex(@"\G/\*[\s\S]*?\*/", RegexOptions.Compiled);

        private readonly Regex MultiLineCommentStartRegex =
            new Regex(@"\G/\*", RegexOptions.Compiled);

        private readonly Regex IdentifierRegex =
            new Regex(@"\G[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);

        private readonly Regex ArrayTypeRegex =
            new Regex(@"\GArray\s*<\s*(?:Int|Float|String|Char|Bool)\s*>", RegexOptions.Compiled);

        private readonly Regex NumberRegex =
            new Regex(@"\G[+-]?[0-9]+(?:\.[0-9]+)?", RegexOptions.Compiled);

        private readonly Regex NumberIdentifierRegex =
            new Regex(@"\G[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);

        private readonly Regex StringRegex =
            new Regex("\\G\"(?:\\\\.|[^\"\\\\\\r\\n])*\"", RegexOptions.Compiled);

        private readonly Regex InvalidStringRegex =
            new Regex("\\G\"(?:\\\\[^\\r\\n]|[^\"\\\\\\r\\n])*(?:\\\\)?", RegexOptions.Compiled);

        private readonly Regex CharRegex =
            new Regex("\\G'(?:\\\\.|[^'\\\\\\r\\n])'", RegexOptions.Compiled);

        private readonly Regex InvalidCharRegex =
            new Regex("\\G'(?:\\\\[^\\r\\n]|[^'\\r\\n])*'?", RegexOptions.Compiled);

        private readonly Regex TwoCharacterOperatorRegex =
            new Regex(@"\G(?:==|!=|<=|>=|\.\.)", RegexOptions.Compiled);

        private readonly Regex OneCharacterOperatorRegex =
            new Regex(@"\G[+\-*/%=<>.,:(){}\[\]]", RegexOptions.Compiled);

        private readonly Regex IdentifierValidationRegex =
            new Regex(@"\A[A-Za-z_][A-Za-z0-9_]*\z", RegexOptions.Compiled);

        private readonly Regex NumberValidationRegex =
            new Regex(@"\A[+-]?[0-9]+(?:\.[0-9]+)?\z", RegexOptions.Compiled);

        public Scanner()
        {
            ReservedWords.Add("Int", Token_Class.Int);
            ReservedWords.Add("Float", Token_Class.Float);
            ReservedWords.Add("String", Token_Class.String);
            ReservedWords.Add("Bool", Token_Class.Bool);
            ReservedWords.Add("Char", Token_Class.Char);
            ReservedWords.Add("Array", Token_Class.Array);
            ReservedWords.Add("Void", Token_Class.Void);

            ReservedWords.Add("var", Token_Class.Var);
            ReservedWords.Add("val", Token_Class.Val);
            ReservedWords.Add("fun", Token_Class.Fun);
            ReservedWords.Add("class", Token_Class.Class);
            ReservedWords.Add("if", Token_Class.If);
            ReservedWords.Add("else", Token_Class.Else);
            ReservedWords.Add("while", Token_Class.While);
            ReservedWords.Add("for", Token_Class.For);
            ReservedWords.Add("in", Token_Class.In);
            ReservedWords.Add("return", Token_Class.Return);
            ReservedWords.Add("true", Token_Class.True);
            ReservedWords.Add("false", Token_Class.False);
            ReservedWords.Add("null", Token_Class.Null);
            ReservedWords.Add("new", Token_Class.New);
            ReservedWords.Add("print", Token_Class.Print);
            ReservedWords.Add("input", Token_Class.Input);
            ReservedWords.Add("break", Token_Class.Break);
            ReservedWords.Add("continue", Token_Class.Continue);
            ReservedWords.Add("init", Token_Class.Init);

            Operators.Add("+", Token_Class.PlusOp);
            Operators.Add("-", Token_Class.MinusOp);
            Operators.Add("*", Token_Class.MultiplyOp);
            Operators.Add("/", Token_Class.DivideOp);
            Operators.Add("%", Token_Class.ModulusOp);
            Operators.Add("=", Token_Class.Assign);
            Operators.Add("<", Token_Class.Less);
            Operators.Add(">", Token_Class.Greater);
            Operators.Add("==", Token_Class.Equal);
            Operators.Add("!=", Token_Class.Notequal);
            Operators.Add("<=", Token_Class.Lessorequal);
            Operators.Add(">=", Token_Class.Greaterorequal);

            Operators.Add("..", Token_Class.Range);
            Operators.Add(".", Token_Class.Dot);
            Operators.Add(",", Token_Class.Comma);
            Operators.Add(":", Token_Class.Colon);
            Operators.Add("(", Token_Class.LParen);
            Operators.Add(")", Token_Class.RParen);
            Operators.Add("{", Token_Class.LBrace);
            Operators.Add("}", Token_Class.RBrace);
            Operators.Add("[", Token_Class.LBracket);
            Operators.Add("]", Token_Class.RBracket);
        }

        public void StartScanning(string SourceCode)
        {
            Tokens.Clear();
            Errors.Error_List.Clear();

            if (SourceCode == null)
                SourceCode = "";

            int i = 0;

            while (i < SourceCode.Length)
            {
                Match match;
                int start = i;

                match = WhiteSpaceRegex.Match(SourceCode, i);
                if (match.Success && match.Index == i)
                {
                    i += match.Length;
                    continue;
                }

                // Comments
                match = LineCommentRegex.Match(SourceCode, i);
                if (match.Success && match.Index == i)
                {
                    Tokens.Add(new Token
                    {
                        lex = match.Value,
                        token_type = Token_Class.LineComment
                    });

                    i += match.Length;
                    continue;
                }

                match = MultiLineCommentRegex.Match(SourceCode, i);
                if (match.Success && match.Index == i)
                {
                    Tokens.Add(new Token
                    {
                        lex = match.Value,
                        token_type = Token_Class.MultiLineComment
                    });

                    i += match.Length;
                    continue;
                }

                match = MultiLineCommentStartRegex.Match(SourceCode, i);
                if (match.Success && match.Index == i)
                {
                    AddError(
                        "Unclosed multiline comment",
                        SourceCode,
                        start);

                    // The rest of the file belongs to this comment.
                    break;
                }

                // Identifiers, reserved words, and Array data types
                match = ArrayTypeRegex.Match(SourceCode, i);
                if (match.Success && match.Index == i)
                {
                    Tokens.Add(new Token
                    {
                        lex = match.Value,
                        token_type = Token_Class.Array
                    });

                    i += match.Length;
                    continue;
                }

                match = IdentifierRegex.Match(SourceCode, i);
                if (match.Success && match.Index == i)
                {
                    string lexeme = match.Value;
                    FindTokenClass(lexeme, SourceCode, start);
                    i += match.Length;
                    continue;
                }

                // Integer and Float literals: [+-]?[0-9]+(\.[0-9]+)?
                match = NumberRegex.Match(SourceCode, i);
                if (match.Success && match.Index == i &&
                    (char.IsDigit(SourceCode[i]) ||
                     ((SourceCode[i] == '+' || SourceCode[i] == '-') &&
                      i + 1 < SourceCode.Length &&
                      char.IsDigit(SourceCode[i + 1]))))
                {
                    string number = match.Value;
                    i += match.Length;

                    Match invalidIdentifier =
                        NumberIdentifierRegex.Match(SourceCode, i);

                    if (invalidIdentifier.Success && invalidIdentifier.Index == i)
                    {
                        i += invalidIdentifier.Length;

                        AddError(
                            "Invalid identifier or number: " +
                            SourceCode.Substring(start, i - start),
                            SourceCode,
                            start);

                        continue;
                    }

                    if (IsNumber(number))
                    {
                        Tokens.Add(new Token
                        {
                            lex = number,
                            token_type = Token_Class.number
                        });
                    }
                    else
                    {
                        AddError(
                            "Invalid number: " + number,
                            SourceCode,
                            start);
                    }

                    continue;
                }

                // String literals
                if (SourceCode[i] == '"')
                {
                    match = StringRegex.Match(SourceCode, i);

                    if (match.Success && match.Index == i)
                    {
                        string lexeme = match.Value;

                        if (IsString(lexeme))
                        {
                            Tokens.Add(new Token
                            {
                                lex = lexeme,
                                token_type = Token_Class.StringLiteral
                            });
                        }

                        i += match.Length;
                    }
                    else
                    {
                        match = InvalidStringRegex.Match(SourceCode, i);
                        string lexeme = match.Success && match.Index == i
                            ? match.Value
                            : SourceCode.Substring(i, 1);

                        AddError(
                            "Invalid or unclosed string: " + lexeme,
                            SourceCode,
                            start);

                        i += lexeme.Length;
                    }

                    continue;
                }

                // Character literals
                if (SourceCode[i] == '\'')
                {
                    match = CharRegex.Match(SourceCode, i);

                    if (match.Success && match.Index == i)
                    {
                        string lexeme = match.Value;

                        if (IsChar(lexeme))
                        {
                            Tokens.Add(new Token
                            {
                                lex = lexeme,
                                token_type = Token_Class.CharLiteral
                            });
                        }

                        i += match.Length;
                    }
                    else
                    {
                        match = InvalidCharRegex.Match(SourceCode, i);
                        string lexeme = match.Success && match.Index == i
                            ? match.Value
                            : SourceCode.Substring(i, 1);

                        AddError(
                            "Invalid or unclosed character: " + lexeme,
                            SourceCode,
                            start);

                        i += lexeme.Length;
                    }

                    continue;
                }

                // Two-character operators and separators
                match = TwoCharacterOperatorRegex.Match(SourceCode, i);
                if (match.Success && match.Index == i &&
                    Operators.ContainsKey(match.Value))
                {
                    FindTokenClass(match.Value, SourceCode, i);
                    i += match.Length;
                    continue;
                }

                // One-character operators and separators
                match = OneCharacterOperatorRegex.Match(SourceCode, i);
                if (match.Success && match.Index == i &&
                    Operators.ContainsKey(match.Value))
                {
                    FindTokenClass(match.Value, SourceCode, i);
                    i += match.Length;
                }
                else
                {
                    AddError(
                        "Unrecognized symbol: " + SourceCode[i],
                        SourceCode,
                        i);

                    i++;
                }
            }
        }

        private void AddError(
            string message,
            string sourceCode,
            int position)
        {
            int line = 1;
            int column = 1;

            for (int j = 0;
                 j < position && j < sourceCode.Length;
                 j++)
            {
                if (sourceCode[j] == '\n')
                {
                    line++;
                    column = 1;
                }
                else if (sourceCode[j] != '\r')
                {
                    column++;
                }
            }

            Errors.Error_List.Add(
                $"Line {line}, Column {column}: {message}");
        }

        private void FindTokenClass(
            string lexeme,
            string sourceCode,
            int position)
        {
            Token token = new Token();
            token.lex = lexeme;

            if (ReservedWords.TryGetValue(
                lexeme, out Token_Class reservedType))
            {
                token.token_type = reservedType;
                Tokens.Add(token);
            }
            else if (IsIdentifier(lexeme))
            {
                token.token_type = Token_Class.Identifier;
                Tokens.Add(token);
            }
            else if (Operators.TryGetValue(
                lexeme, out Token_Class operatorType))
            {
                token.token_type = operatorType;
                Tokens.Add(token);
            }
            else
            {
                AddError(
                    "Unrecognized lexeme: " + lexeme,
                    sourceCode,
                    position);
            }
        }

        private bool IsIdentifier(string lexeme)
        {
            return IdentifierValidationRegex.IsMatch(lexeme);
        }

        private bool IsLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') ||
                   (c >= 'a' && c <= 'z');
        }

        private bool IsNumber(string lexeme)
        {
            return NumberValidationRegex.IsMatch(lexeme);
        }

        private bool IsString(string lexeme)
        {
            return Regex.IsMatch(
                lexeme,
                "\\A\"(?:\\\\.|[^\"\\\\\\r\\n])*\"\\z",
                RegexOptions.Compiled);
        }

        private bool IsChar(string lexeme)
        {
            return Regex.IsMatch(
                lexeme,
                "\\A'(?:\\\\.|[^'\\\\\\r\\n])'\\z",
                RegexOptions.Compiled);
        }
    }
}
