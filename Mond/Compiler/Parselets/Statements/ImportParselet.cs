using System.IO;
using System.Linq;
using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Parselets.Statements
{
    internal class ImportParselet : IStatementParselet
    {
        public Expression Parse(Parser parser, Token token, out bool trailingSemicolon)
        {
            trailingSemicolon = true;

            var moduleName = ParseModuleName(parser, out var moduleNameToken);

            if (token.Type == TokenType.From)
            {
                parser.Take(TokenType.Import);
                parser.Take(TokenType.LeftBrace);

                var fields = VarParselet.ParseObjectDestructuring(parser);
                return new ImportExpression(token, moduleName, fields);
            }

            var moduleBindName = Path.GetFileNameWithoutExtension(moduleName);
            if (string.IsNullOrWhiteSpace(moduleBindName))
            {
                throw new MondCompilerException(moduleNameToken, CompilerError.ImportEmptyModuleFileName);
            }

            if (!char.IsUpper(moduleBindName[0]) || !moduleBindName.All(c => char.IsLetterOrDigit(c) || c == '_'))
            {
                throw new MondCompilerException(moduleNameToken, CompilerError.ImportInvalidBoundName, moduleName, moduleBindName);
            }
            
            return new ImportExpression(token, moduleName, moduleBindName);
        }

        public static string ParseModuleName(Parser parser, out Token moduleNameToken)
        {
            moduleNameToken = parser.Take();
            
            if (moduleNameToken.Type != TokenType.Identifier && moduleNameToken.Type != TokenType.String)
            {
                throw new MondCompilerException(moduleNameToken, CompilerError.ImportExpectedModuleName, moduleNameToken);
            }

            var moduleName = moduleNameToken.Contents;
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                throw new MondCompilerException(moduleNameToken, CompilerError.ImportEmptyModuleName);
            }

            return moduleName;
        }
    }
}
