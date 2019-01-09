﻿////////////////////////////////////////////////////////////////////////////////

using Markdig.Renderers;
using Markdig.Syntax;

namespace MG.MDV
{
    // <p>...</p>

    /// <see cref="Markdig.Renderers.Html.ParagraphRenderer"/>

    public class RendererBlockParagraph : MarkdownObjectRenderer<RendererMarkdown, ParagraphBlock>
    {
        protected override void Write( RendererMarkdown renderer, ParagraphBlock block )
        {
            //if( !renderer.ImplicitParagraph && renderer.EnableHtmlForBlock )
            //{
            //    if( !renderer.IsFirstInContainer )
            //    {
            //        renderer.EnsureLine();
            //    }
            //    renderer.Write( "<p" ).WriteAttributes( obj ).Write( ">" );
            //}

            renderer.WriteLeafBlockInline( block );
            renderer.FlushLine();

            //if( !renderer.ImplicitParagraph )
            //{
            //    if( renderer.EnableHtmlForBlock )
            //    {
            //        renderer.WriteLine( "</p>" );
            //    }
            //    renderer.EnsureLine();
            //}
        }
    }
}
