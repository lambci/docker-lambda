FROM lambci/lambda-base:build

ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_ruby2.5 \
    GEM_HOME=/var/runtime \
    GEM_PATH=/var/task/vendor/bundle/ruby/2.5.0:/opt/ruby/gems/2.5.0 \
    RUBYLIB=/var/task:/var/runtime/lib:/opt/ruby/lib

RUN rm -rf /var/runtime /var/lang /var/rapid && \
  curl https://lambci.s3.amazonaws.com/fs/ruby2.5.tgz | tar -zx -C /

# Add these as a separate layer as they get updated frequently
RUN gem update --system --no-document && \
  gem install --no-document bundler && \
  curl --silent --show-error --retry 5 https://bootstrap.pypa.io/get-pip.py | python && \
  pip install -U awscli boto3 aws-sam-cli==0.16.0 aws-lambda-builders==0.3.0 --no-cache-dir
