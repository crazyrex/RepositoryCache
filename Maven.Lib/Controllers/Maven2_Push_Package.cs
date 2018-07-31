﻿using MultiRepositories.Repositories;
using MultiRepositories.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiRepositories;
using System.IO;
using MavenProtocol.Apis;
using Maven.Services;

namespace Maven.Controllers
{
    public class Maven2_Push_Package: RestAPI
    {
        private IRepositoryEntitiesRepository _repositoryEntitiesRepository;
        private readonly IRequestParser _requestParser;

        public Maven2_Push_Package(
            IRepositoryEntitiesRepository repositoryEntitiesRepository, IRequestParser requestParser, params string[] paths)
            : base(null, paths)
        {
            _repositoryEntitiesRepository = repositoryEntitiesRepository;
            this._requestParser = requestParser;
            SetHandler(Handler);
        }

        private SerializableResponse Handler(SerializableRequest arg)
        {
            var index = _requestParser.Parse(arg);

            return new SerializableResponse();
        }
    }
}
