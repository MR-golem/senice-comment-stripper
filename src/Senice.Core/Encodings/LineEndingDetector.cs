namespace Senice.Core.Encodings;

public static class LineEndingDetector
{
    public static string Detect(string text)
    {
        int crlf = 0;
        int lf = 0;
        int cr = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    crlf++;
                    i++;
                }
                else
                {
                    cr++;
                }
            }
            else if (text[i] == '\n')
            {
                lf++;
            }
        }

        int total = crlf + lf + cr;
        if (total == 0)
            return "none";
        if (crlf == total)
            return "CRLF";
        if (lf == total)
            return "LF";
        if (cr == total)
            return "CR";
        return "mixed";
    }
}
