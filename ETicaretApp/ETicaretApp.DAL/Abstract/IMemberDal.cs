﻿using ETicaretApp.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETicaretApp.DAL.Abstract
{
    public interface IMemberDal:IGenericDal<Member>
    {
        Member GetByGuid(Guid id);
    }
}
