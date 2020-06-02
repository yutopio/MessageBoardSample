using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sample
{
    public class IndexModel
    {
        public MessageModel Input { get; set; }
        public IEnumerable<MessageModel> Posted { get; set; }
    }

    public class MessageModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Key { get; set; }

        [DisplayName("ハンドル名")]
        [Required(ErrorMessage = "ハンドル名を入力してください。")]
        [MaxLength(20, ErrorMessage = "ハンドル名が長すぎます。")]
        public string Username { get; set; }

        [DisplayName("メッセージ")]
        [Required(ErrorMessage = "メッセージを入力してください。")]
        [MaxLength(140, ErrorMessage = "メッセージが長すぎます。")]
        public string Message { get; set; }

        public DateTime Posted { get; set; }
    }
}
