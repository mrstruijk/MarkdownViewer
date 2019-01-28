﻿////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MG.MDV
{
    /******************************************************************************
     * Layout
     ******************************************************************************/

    public class Context
    {
        public StyleCache       StyleLayout;
        public GUIStyle         StyleGUI;
        public IActionHandlers  ActionHandlers;

        public float    LineHeight                      { get { return StyleGUI.lineHeight; } }
        public float    MinWidth                        { get { return LineHeight * 2.0f; } }
        public float    IndentSize                      { get { return LineHeight * 2.0f; } }

        public GUIStyle Apply( Style style )            { StyleGUI = StyleLayout.Apply( style ); return StyleGUI; }
        public GUIStyle Reset()                         { return Apply( new Style() ); }
        public Vector2  CalcSize( GUIContent content )  { return StyleGUI.CalcSize( content ); }
    }

    public class Container
    {
        public List<Row> Rows = new List<Row>();

        public Vector2 Arrange( Context context, Vector2 pos, float maxWidth )
        {
            float oy = pos.y;

            foreach( var row in Rows )
            {
                var size = row.Arrange( context, pos, maxWidth );
                pos.y += size.y;
            }

            return new Vector2( maxWidth, pos.y - oy );
        }
    }

    //------------------------------------------------------------------------------

    public class Row
    {
        public List<Col> Cols = new List<Col>();

        public Row Add( Col col )
        {
            Cols.Add( col );
            return this;
        }

        public Vector2 Arrange( Context context, Vector2 pos, float maxWidth )
        {
            float oy = pos.y;

            foreach( var col in Cols )
            {
                var size = col.Arrange( context, pos, maxWidth );
                pos.y += size.y;
            }

            return new Vector2( maxWidth, pos.y - oy );
        }
    }

    //------------------------------------------------------------------------------

    public class Col
    {
        public List<Block> Blocks = new List<Block>();

        public bool IsEmpty { get { return Blocks.Count == 0; } }

        public Vector2 Arrange( Context context, Vector2 pos, float maxWidth )
        {
            float oy = pos.y;

            foreach( var block in Blocks )
            {
                var size = block.Arrange( context, pos, maxWidth );
                pos.y += size.y;
            }

            return new Vector2( maxWidth, pos.y - oy );
        }
    }

    //------------------------------------------------------------------------------

    public abstract class Block
    {
        public float Indent = 0.0f;

        public abstract Vector2 Arrange( Context context, Vector2 pos, float maxWidth );
        public abstract void Draw( Context context );
    }


    //------------------------------------------------------------------------------
    // hr

    public class BlockLine : Block
    {
        private Rect Rect = new Rect();

        public override void Draw( Context context )
        {
            var rect = new Rect( Rect.position.x, Rect.center.y, Rect.width, 1.0f );
            GUI.Label( rect, string.Empty, GUI.skin.GetStyle( "hr" ) );
        }

        public override Vector2 Arrange( Context context, Vector2 pos, float maxWidth )
        {
            Rect.position = pos;
            Rect.width    = maxWidth;
            Rect.height   = 10.0f;

            return Rect.size;
        }
    }

    //------------------------------------------------------------------------------

    public class BlockSpace : Block
    {
        public override void Draw( Context context )
        {
        }

        public override Vector2 Arrange( Context context, Vector2 pos, float maxWidth )
        {
            return new Vector2( 1.0f, context.LineHeight );
        }
    }

    //------------------------------------------------------------------------------
    // <div>..</div>

    public class BlockContent : Block
    {
        Rect          mRect      = new Rect();
        Content       mPrefix    = null;
        List<Content> mContent   = new List<Content>();

        public bool IsEmpty { get { return mContent.Count == 0; } }
        public bool Highlight  = false;

        public BlockContent( float indent )
        {
            Indent = indent;
        }

        public void Add( Content content )
        {
            mContent.Add( content );
        }

        public void Prefix( Content content )
        {
            mPrefix = content;
        }

        public override Vector2 Arrange( Context context, Vector2 pos, float maxWidth )
        {
            var origin = pos;

            mRect.position = pos;

            pos.x += Indent;
            maxWidth = Mathf.Max( maxWidth - Indent, context.MinWidth );

            // prefix

            if( mPrefix != null )
            {
                mPrefix.Location.x = pos.x - context.IndentSize * 0.5f;
                mPrefix.Location.y = pos.y;
            }

            // content

            if( mContent.Count == 0 )
            {
                return Vector2.zero;
            }

            mContent.ForEach( c => c.Update( context ) );

            var rowWidth   = mContent[0].Width;
            var rowHeight  = mContent[0].Height;
            var startIndex = 0;

            for( var i = 1; i < mContent.Count; i++ )
            {
                var content = mContent[i];

                if( rowWidth + content.Width > maxWidth )
                {
                    LayoutRow( pos, startIndex, i, rowHeight );
                    pos.y += rowHeight;

                    startIndex = i;
                    rowWidth   = content.Width;
                    rowHeight  = content.Height;
                }
                else
                {
                    rowWidth += content.Width;
                    rowHeight = Mathf.Max( rowHeight, content.Height );
                }
            }

            if( startIndex < mContent.Count )
            {
                LayoutRow( pos, startIndex, mContent.Count, rowHeight );
                pos.y += rowHeight;
            }

            mRect.size = new Vector2( maxWidth, pos.y - origin.y );

            return mRect.size;
        }

        void LayoutRow( Vector2 pos, int from, int until, float rowHeight )
        {
            for( var i = from; i < until; i++ )
            {
                var content = mContent[i];

                content.Location.x = pos.x;
                content.Location.y = pos.y + rowHeight - content.Height;

                pos.x += content.Width;
            }
        }

        public override void Draw( Context context  )
        {
            if( Highlight )
            {
                GUI.Box( mRect, string.Empty );
            }

            mContent.ForEach( c => c.Draw( context ) );
            mPrefix?.Draw( context );
        }
    }

    //------------------------------------------------------------------------------

    public abstract class Content
    {
        public Rect         Location;
        public Style        Style;
        public GUIContent   Payload;
        public string       Link;

        public float Width  { get { return Location.width; } }
        public float Height { get { return Location.height; } }

        public Content( GUIContent payload, Style style, string link )
        {
            Payload = payload;
            Style   = style;
            Link    = link;
        }

        public void Draw( Context context )
        {
            if( string.IsNullOrEmpty( Link ) )
            {
                GUI.Label( Location, Payload, context.Apply( Style ) );
            }
            else if( GUI.Button( Location, Payload, context.Apply( Style ) ) )
            {
                if( Regex.IsMatch( Link, @"^\w+:", RegexOptions.Singleline ) )
                {
                    Application.OpenURL( Link );
                }
                else
                {
                    context.ActionHandlers.SelectPage( Link );
                }
            }
        }

        public virtual void Update( Context context )
        {
        }
    }

    public class ContentText : Content
    {
        public ContentText( GUIContent payload, Style style, string link )
            : base( payload, style, link )
        {
        }
    }

    public class ContentImage : Content
    {
        public string URL;
        public string Alt;

        public ContentImage( GUIContent payload, Style style, string link )
            : base( payload, style, link )
        {
        }

        public override void Update( Context context )
        {
            Payload.image = context.ActionHandlers.FetchImage( URL );
            Payload.text  = null;

            if( Payload.image == null )
            {
                context.Apply( Style );
                var text = !string.IsNullOrEmpty( Alt ) ? Alt : URL;
                Payload.text = $"[{text}]";
            }

            Location.size = context.CalcSize( Payload );
        }
    }

    /******************************************************************************
     * Document
     ******************************************************************************/

    public class Layout
    {
        public float Height = 100.0f;

        Context         mContext;
        List<Container> mContainers = new List<Container>();
        List<Block>     mBlocks     = new List<Block>();

        float           mIndent;
        Col             mColumn;
        BlockContent    mCursor;


        public Layout( StyleCache styleCache, IActionHandlers actions )
        {
            mContext = new Context();
            mContext.ActionHandlers = actions;
            mContext.StyleLayout    = styleCache;
            mContext.Reset();

            mIndent     = 0.0f;
            mBlockQuote = false;

            var container = new Container();
            var row = new Row();
            var col = new Col();

            mContainers.Add( container );
            container.Rows.Add( row );
            row.Cols.Add( col );

            mColumn = col;
            NewContentBlock();
        }


        ////////////////////////////////////////////////////////////////////////////////
        // add content

        Style         mStyleLayout  = new Style();
        string        mLink         = null;
        string        mTooltip      = null;
        StringBuilder mWord         = new StringBuilder( 1024 );
        bool          mBlockQuote   = false;

        public bool BlockQuote
        {
            get
            {
                return mBlockQuote;
            }

            set
            {
                mCursor.Highlight = mBlockQuote;
                mBlockQuote = value;
            }
        }


        //------------------------------------------------------------------------------

        public void Text( string text, Style style, string link, string tooltip )
        {
            mContext.Apply( style );

            mStyleLayout = style;
            mLink        = link;
            mTooltip     = tooltip;

            for( var i = 0; i < text.Length; i++ )
            {
                var ch = text[i];

                if( ch == '\n' )
                {
                    AddWord();
                    NewLine();
                }
                else if( char.IsWhiteSpace( ch ) )
                {
                    mWord.Append( ' ' );
                    AddWord();
                }
                else
                {
                    mWord.Append( ch );
                }
            }

            AddWord();
        }

        void AddWord()
        {
            if( mWord.Length == 0 )
            {
                return;
            }

            var payload = new GUIContent( mWord.ToString(), mTooltip );
            var content = new ContentText( payload, mStyleLayout, mLink );

            content.Location.size = mContext.CalcSize( payload );

            mCursor.Add( content );

            mWord.Clear();
        }


        //------------------------------------------------------------------------------

        public void Image( string url, string alt, string title )
        {
            var payload = new GUIContent();
            var content = new ContentImage( payload, mStyleLayout, mLink );

            content.URL     = url;
            content.Alt     = alt;
            payload.tooltip = !string.IsNullOrEmpty( title ) ? title : alt;

            mCursor.Add( content );
        }


        //------------------------------------------------------------------------------

        private void AddBlock( Block block )
        {
            mColumn.Blocks.Add( block );
            mBlocks.Add( block );
        }

        private void NewContentBlock()
        {
            var block = new BlockContent( mIndent );
            AddBlock( block );
            mCursor = block;
            mContext.Reset();
        }

        //------------------------------------------------------------------------------

        public void HorizontalLine()
        {
            AddBlock( new BlockLine() );
            NewContentBlock();
        }

        public void Space()
        {
            AddBlock( new BlockSpace() );
            NewContentBlock();
        }

        public void NewLine()
        {
            if( mCursor.IsEmpty == false )
            {
                NewContentBlock();
            }
        }

        public void Indent()
        {
            mIndent += mContext.IndentSize;
            mCursor.Indent = mIndent;
        }

        public void Outdent()
        {
            mIndent = Mathf.Max( mIndent - mContext.IndentSize, 0.0f );
            mCursor.Indent = mIndent;
        }

        public void Prefix( string text, Style style )
        {
            mContext.Apply( style );

            var payload = new GUIContent( text );
            var content = new ContentText( payload, style, null );
            content.Location.size = mContext.CalcSize( payload );

            mCursor.Prefix( content );
        }


        ////////////////////////////////////////////////////////////////////////////////
        // layout and draw

        public void Arrange( float maxWidth )
        {
            mContext.Reset();
            
            var pos = Vector2.zero;

            foreach( var container in mContainers )
            {
                var size = container.Arrange( mContext, pos, maxWidth );
                pos.y += size.y;
            }

            Height = pos.y;
        }

        public void Draw()
        {
            mContext.Reset();
            mBlocks.ForEach( block => block.Draw( mContext ) );
        }
    }
}
